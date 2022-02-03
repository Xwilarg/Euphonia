const { app, BrowserWindow } = require('electron')

const createWindow = () => {
    const win = new BrowserWindow({
      show: false
    });
    win.maximize();
    win.show();
  
    win.loadFile('index.html');
}

app.whenReady().then(() => {
    createWindow();
});