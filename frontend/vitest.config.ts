import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'

const currentDirectory = path.dirname(fileURLToPath(import.meta.url))

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(currentDirectory, './src'),
    },
  },
  test: {
    experimental: {
      nodeLoader: false,
      viteModuleRunner: false,
    },
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
  },
})
