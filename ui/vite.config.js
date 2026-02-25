import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html'),
        djportal: resolve(__dirname, 'djportal.html'),
        admin: resolve(__dirname, 'admin.html'),
      },
      output: {
        assetFileNames: 'static/[name]-[hash][extname]',
        entryFileNames: 'static/[name]-[hash].js',
        chunkFileNames: 'static/[name]-[hash].js',
      }
    }
  }
})
