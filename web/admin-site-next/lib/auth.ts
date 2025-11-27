// Client-side auth utilities - only check if authenticated, no credentials

export function isAuthenticated(): boolean {
  if (typeof window === 'undefined') return false
  return localStorage.getItem('admin_authenticated') === 'true'
}

export function setAuthenticated(authenticated: boolean): void {
  if (typeof window === 'undefined') return
  if (authenticated) {
    localStorage.setItem('admin_authenticated', 'true')
  } else {
    localStorage.removeItem('admin_authenticated')
  }
}
