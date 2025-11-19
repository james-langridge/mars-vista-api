// Simple hardcoded authentication for admin dashboard
const ADMIN_EMAIL = "admin@marsvista.com"
const ADMIN_PASSWORD = "mars-admin-2025"  // TODO: Change in production!
const ADMIN_API_KEY = "mv_live_23cfbe52447d24f995067e51c6f9e27f554126c8"

export function login(email: string, password: string): boolean {
  if (email === ADMIN_EMAIL && password === ADMIN_PASSWORD) {
    localStorage.setItem('admin_token', 'authenticated')
    localStorage.setItem('admin_api_key', ADMIN_API_KEY)
    return true
  }
  return false
}

export function logout(): void {
  localStorage.removeItem('admin_token')
  localStorage.removeItem('admin_api_key')
}

export function isAuthenticated(): boolean {
  return localStorage.getItem('admin_token') === 'authenticated'
}

export function getAdminApiKey(): string | null {
  return localStorage.getItem('admin_api_key')
}
