/** @type {import('jest').Config} */
const config = {
  preset: 'ts-jest',
  // Use node environment — API routes run in Node.js, not a browser.
  // Node 18+ provides Request, Response, fetch as globals.
  testEnvironment: 'node',
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/src/$1',
    // Stub next/headers (reads cookies from runtime context not available in Jest)
    '^next/headers$': '<rootDir>/src/__tests__/__mocks__/next-headers.ts',
  },
  testMatch: ['**/__tests__/**/*.test.ts', '**/__tests__/**/*.test.tsx'],
  transform: {
    '^.+\\.tsx?$': ['ts-jest', {
      tsconfig: {
        module: 'commonjs',
        moduleResolution: 'node',
      },
    }],
  },
};

module.exports = config;
