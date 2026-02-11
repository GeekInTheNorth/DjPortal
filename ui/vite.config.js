import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path'

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
