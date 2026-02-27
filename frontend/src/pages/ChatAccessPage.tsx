import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getChatWorkspace, updateChatAccess } from '../api/chat';
import { getBusinessUnits } from '../api/businessUnits';
import { useAuth } from '../contexts/AuthContext';

export default function ChatAccessPage() {
  const { isOwner } = useAuth();
  const qc = useQueryClient();
  const [selectedBuId, setSelectedBuId] = useState('');

  const { data: bus } = useQuery({ queryKey: ['bus'], queryFn: getBusinessUnits });

  const { data: workspace, isLoading, error } = useQuery({
    queryKey: ['chat-workspace', selectedBuId],
    queryFn: () => getChatWorkspace(selectedBuId),
    enabled: !!selectedBuId,
  });

  const accessMutation = useMutation({
    mutationFn: ({ personId, hasAccess }: { personId: string; hasAccess: boolean }) =>
      updateChatAccess(selectedBuId, personId, hasAccess),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['chat-workspace', selectedBuId] }),
  });

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-2">Chat Access Control</h1>
      <p className="text-gray-500 mb-6">Manage which staff members can access chat for each Business Unit.</p>

      <div className="mb-6">
        <label className="block text-sm font-medium text-gray-700 mb-2">Select Business Unit</label>
        <select
          value={selectedBuId}
          onChange={e => setSelectedBuId(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm min-w-64"
        >
          <option value="">Choose a Business Unit...</option>
          {bus?.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
      </div>

      {selectedBuId && (
        <>
          {isLoading && <p className="text-gray-400">Loading workspace...</p>}
          {error && <p className="text-red-500 text-sm">Failed to load chat workspace.</p>}
          {workspace && (
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
                <h2 className="font-semibold text-gray-800">Chat Workspace: {workspace.buName}</h2>
                <p className="text-xs text-gray-500 mt-1">Workspace ID: {workspace.workspaceId}</p>
              </div>
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Member</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Workspace Role</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Chat Access</th>
                    {isOwner && <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Action</th>}
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {workspace.members.map(m => (
                    <tr key={m.personId} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{m.firstName} {m.lastName}</td>
                      <td className="px-6 py-4">
                        <span className={`inline-block px-2 py-1 text-xs rounded-full ${
                          m.role === 'Admin' ? 'bg-purple-100 text-purple-700' : 'bg-gray-100 text-gray-700'
                        }`}>{m.role}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-block px-2 py-1 text-xs rounded-full font-medium ${
                          m.hasAccess ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                        }`}>
                          {m.hasAccess ? 'Granted' : 'Denied'}
                        </span>
                      </td>
                      {isOwner && (
                        <td className="px-6 py-4">
                          {m.role !== 'Admin' && (
                            <button
                              onClick={() => accessMutation.mutate({ personId: m.personId, hasAccess: !m.hasAccess })}
                              disabled={accessMutation.isPending}
                              className={`text-sm py-1 px-3 rounded transition-colors disabled:opacity-50 ${
                                m.hasAccess
                                  ? 'bg-red-100 text-red-700 hover:bg-red-200'
                                  : 'bg-green-100 text-green-700 hover:bg-green-200'
                              }`}
                            >
                              {m.hasAccess ? 'Revoke' : 'Grant'} Access
                            </button>
                          )}
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
              {workspace.members.length === 0 && (
                <p className="text-center text-gray-400 text-sm py-8">No members in this workspace yet.</p>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
