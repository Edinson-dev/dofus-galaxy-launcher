# 🌌 Dofus Galaxy — Launcher & Multi-Account Tabs

Launcher oficial y gestor de pestañas multi-cuenta para el servidor privado **Dofus Galaxy (Retro 1.29 / 1.39 Remastered)**.

---

## ✨ Características Principales

### 🎮 1. Dofus Galaxy Launcher (Electron)
- **Interfaz Temática Retro/Cósmica:** Diseñada inspirada en la estética medieval de Dofus 1.29 (marcos de piedra, remaches dorados, barra de progreso animada y pergamino de noticias).
- **Lanzador Independiente:** Inicia el cliente de juego en un proceso completamente independiente del sistema operativo.
- **Bandeja del Sistema (System Tray):** Minimiza automáticamente a la bandeja de Windows al iniciar el juego para ahorrar recursos de CPU y RAM.
- **Acceso Directo a Multi-Cuentas:** Botón dedicado para abrir el gestor WinTabs en cualquier momento.
- **Configuración de Ruta:** Selector de ruta del ejecutable `Dofus.exe` guardado de forma persistente.

### 📑 2. WinTabs Galaxy (C# .NET)
- **Unificación de Cuentas:** Agrupa hasta 8 ventanas de Dofus en una sola ventana organizada con pestañas dinámicas.
- **Detección Automática:** Detecta y ancla nuevas ventanas de Dofus en segundo plano sin intervención manual.
- **⌨ Atajos Configurables:** 
  - Tecla para pasar a la **Siguiente Cuenta** (por defecto `F1`).
  - Tecla para volver a la **Cuenta Anterior** (por defecto `F2`).
  - Salto rápido con `Ctrl + 1` hasta `Ctrl + 8`.
  - Atajos persistentes en `%APPDATA%\DofusGalaxyTabs\hotkeys.cfg`.
- **⚡ Auto-Turno en Combate:** Intercepta la notificación de turno activo de Windows (`HSHELL_FLASH`) para cambiar la pestaña automáticamente al personaje al que le toca jugar.
- **Renombrado de Pestañas:** Doble clic para asignar apodos personalizados a cada personaje.

---

## 🚀 Requisitos y Tecnologías

- **Windows 10 / 11** (64 bits)
- **Node.js** v18+ y npm
- **.NET Framework 4.8** (para compilar WinTabs desde el código fuente)

---

## 🛠️ Instalación y Desarrollo

### 1. Clonar el repositorio
```bash
git clone https://github.com/TU_USUARIO/dofus-galaxy-launcher.git
cd dofus-galaxy-launcher
```

### 2. Instalar dependencias
```bash
npm install
```

### 3. Ejecutar en modo desarrollo
```bash
npm start
```

### 4. Compilar el Instalador para Windows (.exe)
```bash
npm run dist
```
El instalador se generará en la carpeta `dist/` como:
`dist/Dofus Galaxy Launcher Setup 1.0.0.exe`

---

## 📂 Estructura del Proyecto

```text
├── build/
│   └── installer.nsh          # Script de macros NSIS (crea accesos directos duales)
├── tabs-src/
│   └── Program.cs             # Código fuente C# WinForms de DofusGalaxyTabs
├── DofusGalaxyTabs.exe        # Binario precompilado de WinTabs
├── galaxy-banner.jpg          # Banner cósmico con huevo Dofus
├── icon.ico / icon.png        # Iconos oficiales multi-resolución
├── index.html                 # UI del launcher (CSS medieval, canvas de partículas)
├── main.js                    # Proceso principal Electron e IPC handlers
├── preload.js                 # ContextBridge seguro
├── package.json               # Configuración de build de electron-builder
└── README.md
```

---

## 📜 Licencia
Proyecto desarrollado para uso en el servidor privado Dofus Galaxy.
