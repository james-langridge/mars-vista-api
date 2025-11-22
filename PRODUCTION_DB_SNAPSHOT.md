# Production Database Snapshot
**Generated:** 2025-11-22
**Database:** Railway (maglev.proxy.rlwy.net:38340)

## Rover Statistics

| Rover | Photos | Min Sol | Max Sol | Unique Sols | Min Date | Max Date |
|-------|--------|---------|---------|-------------|----------|----------|
| **Curiosity** | 681,750 | 0 | 4,725 | 4,352 | 2012-08-06 | 2025-11-21 |
| **Perseverance** | 456,698 | 0 | 1,690 | 1,501 | 2021-02-18 | 2025-11-20 |
| **Opportunity** | 548,817 | 1 | 5,111 | 4,844 | 2004-01-24 | 2018-06-10 |
| **Spirit** | 301,336 | 1 | 2,209 | 2,123 | 2004-01-03 | 2010-03-21 |
| **TOTAL** | **1,988,601** | - | - | - | - | - |

## Cameras Available

### Curiosity (7 cameras)
- CHEMCAM, FHAZ, MAHLI, MARDI, MAST, NAVCAM, RHAZ

### Perseverance (19 cameras)
- CACHECAM, EDL_DDCAM, EDL_PUCAM1, EDL_PUCAM2, EDL_RDCAM, EDL_RUCAM
- FRONT_HAZCAM_LEFT_A, FRONT_HAZCAM_RIGHT_A
- LCAM, MCZ_LEFT, MCZ_RIGHT
- NAVCAM_LEFT, NAVCAM_RIGHT
- PIXL_MCC, REAR_HAZCAM_LEFT, REAR_HAZCAM_RIGHT
- SHERLOC_ACI, SHERLOC_WATSON, SKYCAM, SUPERCAM_RMI

### Opportunity (6 cameras)
- ENTRY, FHAZ, MINITES, NAVCAM, PANCAM, RHAZ

### Spirit (6 cameras)
- ENTRY, FHAZ, MINITES, NAVCAM, PANCAM, RHAZ

## Sample Photo IDs

| Rover | First Photo ID | Last Photo ID |
|-------|----------------|---------------|
| Curiosity | 451,991 | 2,542,754 |
| Perseverance | 1 | 2,543,105 |
| Opportunity | 1,681,862 | 2,230,688 |
| Spirit | 2,230,689 | 2,532,024 |

## Busiest Sols (Curiosity)

| Sol | Photos | Cameras Used | Earth Date |
|-----|--------|--------------|------------|
| 4,723 | 377 | 3 | 2025-11-18 |
| 4,724 | 262 | 3 | 2025-11-19 |
| 4,721 | 201 | 1 | 2025-11-16 |
| 25 | 200 | 1 | 2012-08-31 |
| 0 | 200 | 1 | 2012-08-06 |

## Popular Locations (Site/Drive for Time Machine)

Curiosity locations with most photos:

| Site | Drive | Photos |
|------|-------|--------|
| 82 | 2,176 | 6,745 |
| 105 | 418 | 3,706 |
| 76 | 3,002 | 3,038 |
| 91 | 516 | 2,308 |
| 31 | 1,330 | 2,271 |
| 6 | 0 | 2,096 |

## Recommended Test Values

**For Sol queries:**
- Early mission: sol=0, sol=1, sol=10, sol=100
- Mid mission: sol=1000, sol=2000, sol=3000
- Recent: sol=4700, sol=4720, sol=4725
- Ranges: sol_min=0&sol_max=10, sol_min=1000&sol_max=1100

**For Date queries:**
- Curiosity landing: 2012-08-06
- Mid-2023: 2023-06-01
- Recent: 2025-11-20, 2025-11-21
- Ranges: 2023-01-01 to 2023-12-31, 2024-01-01 to 2024-11-20

**For Photo IDs:**
- Curiosity: 451991, 1000000, 2000000, 2542754
- Perseverance: 1, 500000, 1000000, 2543105
- Opportunity: 1681862, 2000000, 2230688
- Spirit: 2230689, 2400000, 2532024

**For Time Machine:**
- site=82&drive=2176 (6,745 photos)
- site=105&drive=418 (3,706 photos)
- site=76&drive=3002 (3,038 photos)

**For Cameras:**
- Curiosity: MAST, NAVCAM, FHAZ, CHEMCAM
- Perseverance: MCZ_LEFT, NAVCAM_LEFT, FRONT_HAZCAM_LEFT_A, SHERLOC_WATSON
- Opportunity/Spirit: PANCAM, NAVCAM, FHAZ

## API Key Testing

**Note:** You'll need a valid API key from https://marsvista.dev/dashboard for testing.
Format: `mv_live_<40-char-random-string>`

All production requests require the `X-API-Key` header.
