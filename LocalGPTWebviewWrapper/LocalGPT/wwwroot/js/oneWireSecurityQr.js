window.localGptOneWireSecurity = {
    renderQr(elementId, value, label) {
        const host = document.getElementById(elementId);
        if (!host) return;
        host.replaceChildren();
        if (!value) return;
        try {
            const qr = qrcode(0, 'M');
            qr.addData(String(value), 'Byte');
            qr.make();
            host.innerHTML = qr.createSvgTag({
                cellSize: 4,
                margin: 2,
                scalable: true,
                alt: { text: label || 'LocalGPT 1-Wire security QR code' }
            });
        } catch (error) {
            const message = document.createElement('span');
            message.className = 'onewire-security-error';
            message.textContent = `QR generation failed: ${error?.message || error}`;
            host.appendChild(message);
        }
    }
};
