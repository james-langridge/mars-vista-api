import { useState, useEffect } from 'react'
import { isAuthenticated } from './utils/auth'
import Login from './components/Login'
import Dashboard from './components/Dashboard'

function App() {
  const [authenticated, setAuthenticated] = useState(false)

  useEffect(() => {
    setAuthenticated(isAuthenticated())
  }, [])

  if (!authenticated) {
    return <Login onLogin={() => setAuthenticated(true)} />
  }

  return <Dashboard />
}

export default App
