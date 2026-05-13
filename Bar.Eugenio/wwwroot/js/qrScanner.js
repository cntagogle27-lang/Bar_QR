let streamRef;
let detectorRef;
let timerRef;

export async function startQrScan(videoElement, dotNetRef) {
    if (!("mediaDevices" in navigator) || !("BarcodeDetector" in window)) {
        throw new Error("No compatible");
    }

    await stopQrScan();

    detectorRef = new BarcodeDetector({ formats: ["qr_code"] });
    streamRef = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" }, audio: false });
    videoElement.srcObject = streamRef;
    await videoElement.play();

    timerRef = setInterval(async () => {
        if (!detectorRef || videoElement.readyState < 2) {
            return;
        }

        const codes = await detectorRef.detect(videoElement);
        if (codes.length > 0 && codes[0].rawValue) {
            await dotNetRef.invokeMethodAsync("OnQrDetected", codes[0].rawValue);
        }
    }, 350);
}

export async function stopQrScan() {
    if (timerRef) {
        clearInterval(timerRef);
        timerRef = null;
    }

    if (streamRef) {
        streamRef.getTracks().forEach(track => track.stop());
        streamRef = null;
    }

    detectorRef = null;
}
