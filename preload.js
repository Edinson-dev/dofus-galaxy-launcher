const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  launchGame: (options) => ipcRenderer.invoke('launch-game', options),
  launchTabs: () => ipcRenderer.invoke('launch-tabs'),
  saveConfig: (config) => ipcRenderer.invoke('save-config', config),
  loadConfig: () => ipcRenderer.invoke('load-config'),
  resolvePaths: (clientDir) => ipcRenderer.invoke('resolve-paths', clientDir),
  browseFile: () => ipcRenderer.invoke('browse-file'),
  browseFolder: () => ipcRenderer.invoke('browse-folder'),
  minimizeWindow: () => ipcRenderer.send('minimize-window'),
  closeWindow: () => ipcRenderer.send('close-window'),
});
