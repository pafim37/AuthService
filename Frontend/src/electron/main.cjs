const { app, BrowserWindow } = require('electron');
const path = require('path');
const { pathToFileURL } = require('url');

const angularDevServerArg = '--dev-server=';

function getDevServerUrl() {
  const devServerArgument = process.argv.find((argument) => argument.startsWith(angularDevServerArg));

  if (!devServerArgument) {
    return null;
  }

  return devServerArgument.slice(angularDevServerArg.length);
}

function getBuiltAppUrl() {
  return pathToFileURL(path.join(__dirname, '..', 'dist', 'auth-service', 'browser', 'index.html')).toString();
}

async function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  const devServerUrl = getDevServerUrl();
  await mainWindow.loadURL(devServerUrl ?? getBuiltAppUrl());

  if (devServerUrl) {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }
}

app.whenReady().then(async () => {
  await createWindow();

  app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      await createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});
