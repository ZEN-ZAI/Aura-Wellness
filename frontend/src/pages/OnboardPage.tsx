import { useState } from 'react';
import { Link } from 'react-router-dom';
import { onboard } from '../api/auth';

export default function OnboardPage() {
  const [form, setForm] = useState({
    companyName: '', address: '', contactNumber: '',
    ownerFirstName: '', ownerLastName: '', ownerEmail: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm(f => ({ ...f, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await onboard(form);
      setSuccess(true);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Onboarding failed. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-md text-center">
          <div className="text-green-500 text-5xl mb-4">✓</div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Company Registered!</h2>
          <p className="text-gray-600 mb-2">Your default password is: <strong className="font-mono bg-gray-100 px-2 py-1 rounded">Welcome@123</strong></p>
          <p className="text-gray-500 text-sm mb-6">Please change it after your first login.</p>
          <Link to="/login" className="block w-full py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 text-center">
            Go to Login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12">
      <div className="bg-white p-8 rounded-lg shadow-md w-full max-w-lg">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Register Your Company</h1>
        {error && <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded text-sm">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <section>
            <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-3">Company Info</h2>
            <Field label="Company Name" name="companyName" value={form.companyName} onChange={handleChange} required />
            <Field label="Address" name="address" value={form.address} onChange={handleChange} required />
            <Field label="Contact Number" name="contactNumber" value={form.contactNumber} onChange={handleChange} required />
          </section>
          <section>
            <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide mb-3">Company Owner</h2>
            <div className="grid grid-cols-2 gap-3">
              <Field label="First Name" name="ownerFirstName" value={form.ownerFirstName} onChange={handleChange} required />
              <Field label="Last Name" name="ownerLastName" value={form.ownerLastName} onChange={handleChange} required />
            </div>
            <Field label="Email Address" name="ownerEmail" type="email" value={form.ownerEmail} onChange={handleChange} required />
          </section>
          <button
            type="submit"
            disabled={loading}
            className="w-full py-2 px-4 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50 font-medium transition-colors"
          >
            {loading ? 'Registering...' : 'Register Company'}
          </button>
        </form>
        <p className="mt-4 text-center text-sm text-gray-500">
          Already have an account? <Link to="/login" className="text-indigo-600 hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  );
}

function Field({ label, name, value, onChange, type = 'text', required = false }: {
  label: string; name: string; value: string; onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  type?: string; required?: boolean;
}) {
  return (
    <div className="mb-3">
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      <input
        type={type}
        name={name}
        value={value}
        onChange={onChange}
        required={required}
        className="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm"
      />
    </div>
  );
}
