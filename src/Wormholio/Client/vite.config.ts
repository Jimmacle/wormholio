import {defineConfig, UserConfig} from 'vite';
import {svelte} from '@sveltejs/vite-plugin-svelte';
import {spawn} from 'child_process';
import path from 'path';
import fs from 'fs';

const certFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ''
        ? `${process.env.APPDATA}/ASP.NET/https`
        : `${process.env.HOME}/.aspnet/https`;

const certName = process.env.npm_package_name;
const certFilePath = path.join(certFolder, `${certName}.pem`);
const keyFilePath = path.join(certFolder, `${certName}.key`);

// https://vitejs.dev/config/
export default defineConfig(async () => {
    // make sure dotnet dev certs are available
    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        await new Promise<void>((resolve) => {
            spawn(
                'dotnet',
                ['dev-certs', 'https', '--export-path', certFilePath, '--format', 'Pem', '--no-password'],
                {stdio: 'inherit'})
                .on('exit', (code: any) => {
                    resolve();
                    if (code) {
                        process.exit(code);
                    }
                });
        });
    }

    const config: UserConfig = {
        plugins: [svelte({
            compilerOptions: {
                dev: true,
            }
        })],
        build: {
            manifest: true,
            emptyOutDir: true,
            outDir: '../wwwroot',
        },
        // explicitly set server and HMR port to make HMR work with the .NET proxy
        server: {
            port: 5173,
            strictPort: true,
            hmr: {
                port: 5173
            },
            https: {
                cert: certFilePath,
                key: keyFilePath
            }
        }
    }

    return config;
});
