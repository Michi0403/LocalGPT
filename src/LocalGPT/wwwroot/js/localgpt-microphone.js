// javascript-diagnostics: guarded
// Component-owned capture. PCM WAV avoids browser-specific compressed-audio decoder dependencies.
export async function create(owner, element) {
    let stream, context, source, processor, observer;
    let samples = [], frames = 0, active = false, disposed = false;
    const maximumBytes = 16 * 1024 * 1024;
    const release = async () => {
        active = false;
        if (processor) { processor.onaudioprocess = null; processor.disconnect(); processor = null; }
        if (source) { source.disconnect(); source = null; }
        if (stream) { stream.getTracks().forEach(track => track.stop()); stream = null; }
        if (context) { await context.close(); context = null; }
    };
    const finish = async (submit) => {
        if (!active) return;
        const rate = context.sampleRate;
        const blocks = samples;
        const count = frames;
        samples = []; frames = 0;
        await release();
        if (!submit || disposed || !count) return;
        const bytes = new Uint8Array(44 + count * 2);
        const view = new DataView(bytes.buffer);
        const label = (offset, value) => [...value].forEach((char, i) => view.setUint8(offset + i, char.charCodeAt(0)));
        label(0, "RIFF"); view.setUint32(4, bytes.length - 8, true); label(8, "WAVE");
        label(12, "fmt "); view.setUint32(16, 16, true); view.setUint16(20, 1, true);
        view.setUint16(22, 1, true); view.setUint32(24, rate, true); view.setUint32(28, rate * 2, true);
        view.setUint16(32, 2, true); view.setUint16(34, 16, true); label(36, "data"); view.setUint32(40, count * 2, true);
        let offset = 44;
        for (const block of blocks) for (const sample of block) {
            const value = Math.max(-1, Math.min(1, sample));
            view.setInt16(offset, Math.round(value * (value < 0 ? 32768 : 32767)), true); offset += 2;
        }
        await owner.invokeMethodAsync("RecordingReady", DotNet.createJSStreamReference(bytes), bytes.length);
    };
    const stopOnNavigation = () => { void finish(false); };
    window.addEventListener("pagehide", stopOnNavigation);
    observer = new MutationObserver(() => { if (!element.isConnected) void api.dispose(); });
    observer.observe(document.body, { childList: true, subtree: true });
    const api = {
        async start(maximumSeconds) {
            if (active || disposed) return;
            try {
                stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                if (disposed) { await release(); return; }
                context = new AudioContext();
                await context.resume();
                source = context.createMediaStreamSource(stream);
                processor = context.createScriptProcessor(4096, 1, 1);
                const limit = Math.min(Math.floor((maximumBytes - 44) / 2), Math.floor(context.sampleRate * maximumSeconds));
                samples = []; frames = 0; active = true;
                processor.onaudioprocess = event => {
                    if (!active) return;
                    const block = event.inputBuffer.getChannelData(0).slice(0, Math.max(0, limit - frames));
                    samples.push(block); frames += block.length;
                    if (frames >= limit) void finish(true).catch(async () => {
                        console.error("Microphone transfer failed; audio omitted.");
                        await release();
                    });
                };
                source.connect(processor); processor.connect(context.destination);
                stream.getAudioTracks().forEach(track => track.onended = () => { void finish(false); });
            } catch (error) { await release(); throw error; }
        },
        async stop() { await finish(true); },
        async cancel() { await finish(false); },
        async dispose() {
            disposed = true; observer?.disconnect(); window.removeEventListener("pagehide", stopOnNavigation);
            samples = []; frames = 0; await release();
        }
    };
    return DotNet.createJSObjectReference(api);
}
