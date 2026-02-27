import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { getBusinessUnits } from '../api/businessUnits';
import { getStaff } from '../api/staff';

export default function DashboardPage() {
  const { user } = useAuth();
  const { data: bus } = useQuery({ queryKey: ['bus'], queryFn: getBusinessUnits });
  const { data: staff } = useQuery({ queryKey: ['staff'], queryFn: getStaff });

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Dashboard</h1>
      <p className="text-gray-500 mb-8">Welcome back, {user?.firstName} {user?.lastName}</p>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <StatCard label="Business Units" value={bus?.length ?? 0} color="indigo" />
        <StatCard label="Staff Members" value={staff?.length ?? 0} color="green" />
        <StatCard label="Your Role" value={user?.role ?? ''} color="purple" />
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Business Units</h2>
        {bus && bus.length > 0 ? (
          <ul className="space-y-2">
            {bus.map(bu => (
              <li key={bu.id} className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
                <span className="text-gray-800 font-medium">{bu.name}</span>
                <span className="text-xs text-gray-400">{new Date(bu.createdAt).toLocaleDateString()}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-gray-400 text-sm">No business units yet.</p>
        )}
      </div>
    </div>
  );
}

function StatCard({ label, value, color }: { label: string; value: number | string; color: string }) {
  const colors: Record<string, string> = {
    indigo: 'bg-indigo-50 text-indigo-700',
    green: 'bg-green-50 text-green-700',
    purple: 'bg-purple-50 text-purple-700',
  };
  return (
    <div className={`rounded-lg p-6 ${colors[color]}`}>
      <p className="text-sm font-medium opacity-80">{label}</p>
      <p className="text-3xl font-bold mt-1">{value}</p>
    </div>
  );
}
