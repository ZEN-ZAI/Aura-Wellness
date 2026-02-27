import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getStaff, createStaff, updateRole } from '../api/staff';
import { getBusinessUnits } from '../api/businessUnits';
import { useAuth } from '../contexts/AuthContext';

export default function StaffPage() {
  const { isOwner } = useAuth();
  const qc = useQueryClient();
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ firstName: '', lastName: '', buId: '', email: '', role: 'Staff' });

  const { data: staff, isLoading } = useQuery({ queryKey: ['staff'], queryFn: getStaff });
  const { data: bus } = useQuery({ queryKey: ['bus'], queryFn: getBusinessUnits });

  const createMutation = useMutation({
    mutationFn: () => createStaff(form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['staff'] });
      setShowModal(false);
      setForm({ firstName: '', lastName: '', buId: '', email: '', role: 'Staff' });
      setError('');
    },
    onError: (err: any) => setError(err.response?.data?.error || 'Failed to create staff.'),
  });

  const roleMutation = useMutation({
    mutationFn: ({ personId, role }: { personId: string; role: string }) => updateRole(personId, role),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['staff'] }),
  });

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    setForm(f => ({ ...f, [e.target.name]: e.target.value }));
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Staff Management</h1>
        {isOwner && (
          <button
            onClick={() => setShowModal(true)}
            className="py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm font-medium transition-colors"
          >
            + Add Staff Member
          </button>
        )}
      </div>

      {isLoading ? <p className="text-gray-400">Loading...</p> : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Name', 'Email', 'Business Unit', 'Role', isOwner ? 'Actions' : ''].map(h => h && (
                  <th key={h} className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {staff?.map(s => (
                <tr key={s.profileId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{s.firstName} {s.lastName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{s.email}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{s.buName}</td>
                  <td className="px-6 py-4">
                    <span className={`inline-block px-2 py-1 text-xs rounded-full ${
                      s.role === 'Owner' ? 'bg-purple-100 text-purple-700' :
                      s.role === 'Admin' ? 'bg-blue-100 text-blue-700' :
                      'bg-gray-100 text-gray-700'
                    }`}>{s.role}</span>
                  </td>
                  {isOwner && s.role !== 'Owner' && (
                    <td className="px-6 py-4">
                      <select
                        value={s.role}
                        onChange={e => roleMutation.mutate({ personId: s.personId, role: e.target.value })}
                        className="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      >
                        <option value="Admin">Admin</option>
                        <option value="Staff">Staff</option>
                      </select>
                    </td>
                  )}
                  {isOwner && s.role === 'Owner' && <td className="px-6 py-4" />}
                </tr>
              ))}
            </tbody>
          </table>
          {(!staff || staff.length === 0) && <p className="text-center text-gray-400 text-sm py-8">No staff found.</p>}
        </div>
      )}

      {showModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold">Add Staff Member</h2>
              <button onClick={() => { setShowModal(false); setError(''); }} className="text-gray-400 hover:text-gray-600">✕</button>
            </div>
            {error && <div className="mb-3 p-3 bg-red-50 border border-red-200 text-red-700 rounded text-sm">{error}</div>}
            <div className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <Field label="First Name" name="firstName" value={form.firstName} onChange={handleChange} />
                <Field label="Last Name" name="lastName" value={form.lastName} onChange={handleChange} />
              </div>
              <Field label="Email" name="email" type="email" value={form.email} onChange={handleChange} />
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Business Unit</label>
                <select name="buId" value={form.buId} onChange={handleChange}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm">
                  <option value="">Select BU...</option>
                  {bus?.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Role</label>
                <select name="role" value={form.role} onChange={handleChange}
                  className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm">
                  <option value="Admin">Admin</option>
                  <option value="Staff">Staff</option>
                </select>
              </div>
            </div>
            <div className="flex gap-3 justify-end mt-4">
              <button onClick={() => setShowModal(false)} className="py-2 px-4 border border-gray-300 text-gray-700 rounded hover:bg-gray-50 text-sm">Cancel</button>
              <button
                onClick={() => createMutation.mutate()}
                disabled={createMutation.isPending || !form.firstName || !form.email || !form.buId}
                className="py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50 text-sm"
              >
                {createMutation.isPending ? 'Adding...' : 'Add Staff'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Field({ label, name, value, onChange, type = 'text' }: {
  label: string; name: string; value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void; type?: string;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      <input type={type} name={name} value={value} onChange={onChange}
        className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm" />
    </div>
  );
}
