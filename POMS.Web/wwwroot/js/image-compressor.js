/**
 * Client-side Image Compression & Optimization Utility
 * Automatically compresses selected images to under specified size (default: 500KB)
 * before form submission, preventing upload timeouts and server/proxy payload errors.
 */
(function () {
    const DEFAULT_MAX_KB = 490; // Target strictly under 500KB (buffer for 500KB limit)
    const MAX_DIMENSION = 1280; // High quality headshot/profile dimension

    function formatBytes(bytes) {
        if (bytes === 0) return '0 KB';
        const k = 1024;
        const kb = bytes / k;
        if (kb < 1024) return kb.toFixed(1) + ' KB';
        return (kb / 1024).toFixed(2) + ' MB';
    }

    function initImageCompressor(input) {
        if (input.dataset.compressorInitialized) return;
        input.dataset.compressorInitialized = 'true';

        const maxKb = parseInt(input.dataset.compressMaxKb || DEFAULT_MAX_KB, 10);
        const maxBytes = maxKb * 1024;
        const form = input.closest('form');

        // Locate or create UI container for preview & status
        let feedbackContainer = document.getElementById(input.name + 'Feedback');
        if (!feedbackContainer) {
            feedbackContainer = document.createElement('div');
            feedbackContainer.className = 'image-optimizer-feedback mt-2';
            input.parentNode.appendChild(feedbackContainer);
        }

        let isCompressing = false;
        let pendingSubmit = false;

        function setStatus(html) {
            feedbackContainer.innerHTML = html;
        }

        function setFormSubmitting(disabled) {
            if (!form) return;
            const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
            submitButtons.forEach(btn => {
                btn.disabled = disabled;
            });
        }

        input.addEventListener('change', async function () {
            const files = input.files;
            if (!files || files.length === 0) {
                setStatus('');
                return;
            }

            const file = files[0];
            if (!file.type.startsWith('image/')) {
                setStatus('<div class="alert alert-warning py-1 px-2 small mb-0">Please select an image file (.jpg, .png, etc.).</div>');
                return;
            }

            isCompressing = true;
            setFormSubmitting(true);

            setStatus(`
                <div class="d-flex align-items-center gap-2 p-2 rounded bg-light border border-info-subtle text-info-emphasis small">
                    <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                    <div>
                        <strong>Optimizing photo...</strong>
                        <div class="text-muted" style="font-size: 0.8rem;">Reducing size from ${formatBytes(file.size)} to under 500KB</div>
                    </div>
                </div>
            `);

            try {
                const compressedFile = await processImage(file, maxBytes);

                // Update input file list using DataTransfer
                const dt = new DataTransfer();
                dt.items.add(compressedFile);
                input.files = dt.files;

                const previewUrl = URL.createObjectURL(compressedFile);
                const originalSizeText = formatBytes(file.size);
                const compressedSizeText = formatBytes(compressedFile.size);

                const reductionNotice = file.size > compressedFile.size
                    ? `<span class="badge bg-success-subtle text-success border border-success-subtle ms-1">Reduced by ${Math.round((1 - compressedFile.size / file.size) * 100)}%</span>`
                    : '';

                setStatus(`
                    <div class="d-flex align-items-center gap-3 p-2 rounded bg-light border border-success-subtle mt-2">
                        <img src="${previewUrl}" alt="Preview" class="rounded shadow-sm border" style="width: 56px; height: 56px; object-fit: cover; flex-shrink: 0;" />
                        <div class="small">
                            <div class="text-success fw-bold d-flex align-items-center gap-1">
                                <span>✓ Photo ready for upload</span>
                                ${reductionNotice}
                            </div>
                            <div class="text-muted" style="font-size: 0.8rem;">
                                Size: <strong class="text-dark">${compressedSizeText}</strong> (Under 500KB limit)
                                ${file.size > compressedFile.size ? `· Original: ${originalSizeText}` : ''}
                            </div>
                        </div>
                    </div>
                `);
            } catch (err) {
                console.error('Image compression failed:', err);
                setStatus(`
                    <div class="alert alert-warning py-1 px-2 small mb-0">
                        Could not automatically optimize image. Will try uploading original: ${formatBytes(file.size)}.
                    </div>
                `);
            } finally {
                isCompressing = false;
                setFormSubmitting(false);

                if (pendingSubmit && form) {
                    pendingSubmit = false;
                    form.requestSubmit();
                }
            }
        });

        if (form) {
            form.addEventListener('submit', function (e) {
                if (isCompressing) {
                    e.preventDefault();
                    pendingSubmit = true;
                    setStatus(`
                        <div class="d-flex align-items-center gap-2 p-2 rounded bg-warning-subtle text-warning-emphasis small">
                            <div class="spinner-border spinner-border-sm text-warning" role="status"></div>
                            <div>Compressing photo to under 500KB. Form will submit automatically once complete...</div>
                        </div>
                    `);
                }
            });
        }
    }

    function processImage(file, targetMaxBytes) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            const objectUrl = URL.createObjectURL(file);

            img.onload = async () => {
                URL.revokeObjectURL(objectUrl);

                try {
                    let width = img.naturalWidth;
                    let height = img.naturalHeight;

                    // If image is already smaller than targetMaxBytes and reasonable resolution, keep it
                    if (file.size <= targetMaxBytes && width <= MAX_DIMENSION && height <= MAX_DIMENSION && (file.type === 'image/jpeg' || file.type === 'image/png')) {
                        resolve(file);
                        return;
                    }

                    // Scale down dimensions if exceeds MAX_DIMENSION
                    if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
                        const ratio = Math.min(MAX_DIMENSION / width, MAX_DIMENSION / height);
                        width = Math.round(width * ratio);
                        height = Math.round(height * ratio);
                    }

                    const canvas = document.createElement('canvas');
                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext('2d');

                    // Fill white background for any transparency (JPEG has no alpha channel)
                    ctx.fillStyle = '#FFFFFF';
                    ctx.fillRect(0, 0, width, height);
                    ctx.drawImage(img, 0, 0, width, height);

                    // Step 1: Try with standard quality 0.85
                    let quality = 0.85;
                    let blob = await getCanvasBlob(canvas, quality);

                    // Step 2: Progressively step down quality if over max target bytes
                    const qualitySteps = [0.75, 0.65, 0.55, 0.45, 0.35];
                    let stepIndex = 0;
                    while (blob.size > targetMaxBytes && stepIndex < qualitySteps.length) {
                        quality = qualitySteps[stepIndex];
                        blob = await getCanvasBlob(canvas, quality);
                        stepIndex++;
                    }

                    // Step 3: If still over max target bytes, scale dimensions down further
                    let currentCanvas = canvas;
                    while (blob.size > targetMaxBytes && currentCanvas.width > 400 && currentCanvas.height > 400) {
                        const nextCanvas = document.createElement('canvas');
                        nextCanvas.width = Math.round(currentCanvas.width * 0.75);
                        nextCanvas.height = Math.round(currentCanvas.height * 0.75);
                        const nCtx = nextCanvas.getContext('2d');
                        nCtx.fillStyle = '#FFFFFF';
                        nCtx.fillRect(0, 0, nextCanvas.width, nextCanvas.height);
                        nCtx.drawImage(currentCanvas, 0, 0, nextCanvas.width, nextCanvas.height);
                        currentCanvas = nextCanvas;
                        blob = await getCanvasBlob(currentCanvas, 0.6);
                    }

                    const cleanBaseName = file.name.substring(0, file.name.lastIndexOf('.')) || 'photo';
                    const compressedFile = new File([blob], `${cleanBaseName}.jpg`, {
                        type: 'image/jpeg',
                        lastModified: Date.now()
                    });

                    resolve(compressedFile);
                } catch (err) {
                    reject(err);
                }
            };

            img.onerror = () => {
                URL.revokeObjectURL(objectUrl);
                reject(new Error('Failed to load image file.'));
            };

            img.src = objectUrl;
        });
    }

    function getCanvasBlob(canvas, quality) {
        return new Promise(resolve => {
            canvas.toBlob(blob => resolve(blob), 'image/jpeg', quality);
        });
    }

    function scanInputs() {
        // Targets player profilePic or any input with data-compress-max-kb
        const inputs = document.querySelectorAll('input[type="file"][name="profilePic"], input[type="file"][data-compress-max-kb]');
        inputs.forEach(initImageCompressor);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scanInputs);
    } else {
        scanInputs();
    }

    // Expose utility globally
    window.ImageCompressor = {
        init: initImageCompressor,
        scan: scanInputs,
        processImage: processImage
    };
})();
