const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  launchGame: (gamePath) => ipcRenderer.invoke('launch-game', gamePath),
  launchTabs: () => ipcRenderer.invoke('launch-tabs'),
  saveConfig: (config) => ipcRenderer.invoke('save-config', config),
  loadConfig: () => ipcRenderer.invoke('load-config'),
  browseFile: () => ipcRenderer.invoke('browse-file'),
  minimizeWindow: () => ipcRenderer.send('minimize-window'),
  closeWindow: () => ipcRenderer.send('close-window'),
});
