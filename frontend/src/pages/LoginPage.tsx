import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { login } from '../api/auth';
import { useAuth } from '../contexts/AuthContext';
import type { AuthUser, BuChoice } from '../types';

export default function LoginPage() {
  const navigate = useNavigate();
  const { login: storeLogin } = useAuth();
  const [email, setEmail] = useState('Welcome@example123');
  const [password, setPassword] = useState('Password@123');
  const [buChoices, setBuChoices] = useState<BuChoice[] | null>(null);
  const [selectedBuId, setSelectedBuId] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const buId = buChoices ? selectedBuId : undefined;
      const res = await login(email, password, buId);

      if (res.requiresBuSelection && res.choices) {
        setBuChoices(res.choices);
        setSelectedBuId(res.choices[0]?.buId ?? '');
        setLoading(false);
        return;
      }

      const user: AuthUser = {
        token: res.token!,
        personId: res.personId!,
        buId: res.buId!,
        companyId: res.companyId!,
        role: res.role as AuthUser['role'],
        firstName: res.firstName!,
        lastName: res.lastName!,
      };
      storeLogin(user);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.error || 'Invalid credentials.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-sm">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Sign In</h1>
        {error && <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded text-sm">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email Address</label>
            <input
              defaultValue={email}
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              disabled={!!buChoices}
              className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm disabled:bg-gray-50"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              defaultValue={password}
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm"
            />
          </div>

          {buChoices && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Select Business Unit</label>
              <select
                value={selectedBuId}
                onChange={e => setSelectedBuId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm"
              >
                {buChoices.map(c => (
                  <option key={c.buId} value={c.buId}>{c.buName}</option>
                ))}
              </select>
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50 font-medium transition-colors"
          >
            {loading ? 'Signing in...' : buChoices ? 'Continue' : 'Sign In'}
          </button>
        </form>
        <p className="mt-4 text-center text-sm text-gray-500">
          New company? <Link to="/onboard" className="text-indigo-600 hover:underline">Register here</Link>
        </p>
      </div>
    </div>
  );
}
