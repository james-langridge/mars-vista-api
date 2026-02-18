#!/usr/bin/env python3
"""
Panorama stitching script for Mars Vista API.

Reads JSON from stdin with image paths and output path,
stitches images using OpenCV, writes result as JPEG,
and outputs JSON result to stdout.

Input JSON:
  { "image_paths": ["path1.jpg", ...], "output_path": "out.jpg" }

Output JSON:
  { "status": "success", "width": N, "height": N, "size_bytes": N }
  { "status": "failed", "error": "..." }
"""

import json
import os
import sys

import cv2
import numpy as np


def stitch_images(image_paths, output_path):
    """Load images, stitch them, and save the result."""
    images = []
    for path in image_paths:
        img = cv2.imread(path)
        if img is None:
            return {"status": "failed", "error": f"Failed to load image: {path}"}
        images.append(img)

    if len(images) < 2:
        return {"status": "failed", "error": "Need at least 2 images to stitch"}

    stitcher = cv2.Stitcher_create(cv2.STITCHER_PANORAMA)
    status, result = stitcher.stitch(images)

    status_messages = {
        cv2.STITCHER_OK: "success",
        cv2.STITCHER_ERR_NEED_MORE_IMGS: "Not enough overlap between images for stitching",
        cv2.STITCHER_ERR_HOMOGRAPHY_EST_FAIL: "Homography estimation failed - images may not overlap",
        cv2.STITCHER_ERR_CAMERA_PARAMS_ADJUST_FAIL: "Camera parameter adjustment failed",
    }

    if status != cv2.STITCHER_OK:
        error_msg = status_messages.get(status, f"Stitching failed with status code {status}")
        return {"status": "failed", "error": error_msg}

    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    # Save as JPEG with quality 92
    cv2.imwrite(output_path, result, [cv2.IMWRITE_JPEG_QUALITY, 92])

    height, width = result.shape[:2]
    size_bytes = os.path.getsize(output_path)

    return {
        "status": "success",
        "width": width,
        "height": height,
        "size_bytes": size_bytes,
    }


def main():
    try:
        input_data = json.loads(sys.stdin.read())
    except json.JSONDecodeError as e:
        print(json.dumps({"status": "failed", "error": f"Invalid JSON input: {e}"}))
        sys.exit(1)

    image_paths = input_data.get("image_paths", [])
    output_path = input_data.get("output_path", "")

    if not image_paths:
        print(json.dumps({"status": "failed", "error": "No image paths provided"}))
        sys.exit(1)

    if not output_path:
        print(json.dumps({"status": "failed", "error": "No output path provided"}))
        sys.exit(1)

    result = stitch_images(image_paths, output_path)
    print(json.dumps(result))


if __name__ == "__main__":
    main()
