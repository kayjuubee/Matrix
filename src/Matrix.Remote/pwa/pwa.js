(() => {
  const help = document.getElementById('installHelp'), button = document.getElementById('installApp');
  let prompt = null;
  const standalone = () => matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
  function describe() {
    if (standalone()) { document.getElementById('appInstall').hidden = true; return; }
    help.textContent = !isSecureContext
      ? 'Die Steuerung funktioniert hier im Browser. Für die App-Installation öffne die private HTTPS-Adresse deiner Fotobox, z. B. über Tailscale Serve.'
      : /iPad|iPhone|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
        ? 'In Safari: Teilen → Zum Home-Bildschirm → als Web-App hinzufügen. Danach über das Matrix-Symbol öffnen und den Zugangscode eingeben.'
        : 'Im Browser-Menü „App installieren“ oder „Zum Startbildschirm hinzufügen“ wählen. Falls verfügbar, erscheint hier auch eine Installationsschaltfläche.';
  }
  describe();
  window.addEventListener('beforeinstallprompt', event => { event.preventDefault(); prompt = event; button.hidden = false; });
  button.onclick = async () => {
    if (!prompt) return;
    const current = prompt; prompt = null; button.hidden = true;
    try { await current.prompt(); await current.userChoice; } catch { describe(); }
  };
  window.addEventListener('appinstalled', () => { button.hidden = true; help.textContent = 'Matrix wurde installiert. Öffne die App über das Symbol auf deinem Startbildschirm.'; });
  if ('serviceWorker' in navigator && isSecureContext)
    navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(() => { help.textContent = 'Die App-Vorbereitung ist fehlgeschlagen. Du kannst weiter im Browser steuern. Bitte die Seite erneut öffnen.'; });
})();
