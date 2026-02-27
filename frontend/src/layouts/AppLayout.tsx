import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function AppLayout() {
  const { user, logout, isOwner } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login');
  }

  return (
    <div className="flex h-screen bg-gray-100">
      <aside className="w-64 bg-indigo-800 text-white flex flex-col">
        <div className="p-6 border-b border-indigo-700">
          <h1 className="text-xl font-bold">Aura Wellness</h1>
          <p className="text-indigo-300 text-sm mt-1">{user?.firstName} {user?.lastName}</p>
          <span className="inline-block mt-1 px-2 py-0.5 text-xs bg-indigo-600 rounded">{user?.role}</span>
        </div>
        <nav className="flex-1 p-4 space-y-1">
          <NavLink to="/dashboard">Dashboard</NavLink>
          <NavLink to="/business-units">Business Units</NavLink>
          {isOwner && <NavLink to="/staff">Staff Management</NavLink>}
          <NavLink to="/chat-access">Chat Access</NavLink>
        </nav>
        <div className="p-4">
          <button
            onClick={handleLogout}
            className="w-full py-2 px-4 bg-indigo-700 hover:bg-indigo-600 rounded text-sm transition-colors"
          >
            Sign Out
          </button>
        </div>
      </aside>
      <main className="flex-1 overflow-auto p-8">
        <Outlet />
      </main>
    </div>
  );
}

function NavLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <Link
      to={to}
      className="block px-4 py-2 rounded hover:bg-indigo-700 text-sm transition-colors"
    >
      {children}
    </Link>
  );
}
