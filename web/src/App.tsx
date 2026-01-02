import { Routes, Route, Link } from 'react-router-dom'
import ServiceListPage from './pages/ServiceListPage'
import ServiceDetailPage from './pages/ServiceDetailPage'
import RunbookEditorPage from './pages/RunbookEditorPage'

function App() {
  return (
    <div className="app">
      <nav className="nav">
        <h1>ShipSquire</h1>
        <Link to="/services">Services</Link>
      </nav>
      <main className="main">
        <Routes>
          <Route path="/" element={<ServiceListPage />} />
          <Route path="/services" element={<ServiceListPage />} />
          <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
          <Route path="/runbooks/:runbookId" element={<RunbookEditorPage />} />
        </Routes>
      </main>
    </div>
  )
}

export default App
