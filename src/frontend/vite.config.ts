import { loadEnv } from 'vite'
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import { parseAuthenticationEnvironment } from './src/auth/authEnvironment.ts'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  if (command === 'build') {
    parseAuthenticationEnvironment(loadEnv(mode, process.cwd(), 'VITE_'));
  }

  return {
    plugins: [react()],
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  };
})
