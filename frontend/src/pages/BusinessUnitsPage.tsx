import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getBusinessUnits, createBusinessUnit } from '../api/businessUnits';
import { useAuth } from '../contexts/AuthContext';

export default function BusinessUnitsPage() {
  const { isOwner } = useAuth();
  const qc = useQueryClient();
  const [showModal, setShowModal] = useState(false);
  const [name, setName] = useState('');
  const [error, setError] = useState('');

  const { data: bus, isLoading } = useQuery({ queryKey: ['bus'], queryFn: getBusinessUnits });

  const mutation = useMutation({
    mutationFn: () => createBusinessUnit(name),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['bus'] });
      setShowModal(false);
      setName('');
      setError('');
    },
    onError: (err: any) => {
      setError(err.response?.data?.error || 'Failed to create business unit.');
    },
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Business Units</h1>
        {isOwner && (
          <button
            onClick={() => setShowModal(true)}
            className="py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-sm font-medium transition-colors"
          >
            + New Business Unit
          </button>
        )}
      </div>

      {isLoading ? (
        <p className="text-gray-400">Loading...</p>
      ) : (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {bus?.map(bu => (
                <tr key={bu.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{bu.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{new Date(bu.createdAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {(!bus || bus.length === 0) && (
            <p className="text-center text-gray-400 text-sm py-8">No business units found.</p>
          )}
        </div>
      )}

      {showModal && (
        <Modal title="New Business Unit" onClose={() => { setShowModal(false); setError(''); setName(''); }}>
          {error && <div className="mb-3 p-3 bg-red-50 border border-red-200 text-red-700 rounded text-sm">{error}</div>}
          <label className="block text-sm font-medium text-gray-700 mb-1">Business Unit Name</label>
          <input
            type="text"
            value={name}
            onChange={e => setName(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm mb-4"
            placeholder="e.g., Marketing Team"
          />
          <div className="flex gap-3 justify-end">
            <button onClick={() => setShowModal(false)} className="py-2 px-4 border border-gray-300 text-gray-700 rounded hover:bg-gray-50 text-sm">Cancel</button>
            <button
              onClick={() => mutation.mutate()}
              disabled={mutation.isPending || !name.trim()}
              className="py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50 text-sm"
            >
              {mutation.isPending ? 'Creating...' : 'Create'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}

function Modal({ title, children, onClose }: { title: string; children: React.ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>
        {children}
      </div>
    </div>
  );
}
