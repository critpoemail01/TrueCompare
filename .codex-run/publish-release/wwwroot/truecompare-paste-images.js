const connections = new Map();

export function connect(elementId, dotNetReference, maxBytes) {
    disconnect(elementId);

    const element = document.getElementById(elementId);
    if (!element) {
        return false;
    }

    const onPaste = async (event) => {
        const file = findImageFile(event.clipboardData);
        if (!file) {
            return;
        }

        event.preventDefault();

        if (file.size > maxBytes) {
            await dotNetReference.invokeMethodAsync("HandlePastedImageRejectedAsync", "too-large");
            return;
        }

        try {
            const base64Image = await readAsBase64(file);
            await dotNetReference.invokeMethodAsync(
                "HandlePastedImageAsync",
                createFileName(file),
                file.type || "image/png",
                base64Image,
                file.size);
        } catch {
            await dotNetReference.invokeMethodAsync("HandlePastedImageRejectedAsync", "invalid");
        }
    };

    element.addEventListener("paste", onPaste);
    connections.set(elementId, { element, onPaste });

    return true;
}

export function disconnect(elementId) {
    const connection = connections.get(elementId);
    if (!connection) {
        return;
    }

    connection.element.removeEventListener("paste", connection.onPaste);
    connections.delete(elementId);
}

function findImageFile(clipboardData) {
    if (!clipboardData) {
        return null;
    }

    const files = Array.from(clipboardData.files || []);
    const directFile = files.find((file) => file.type?.startsWith("image/"));
    if (directFile) {
        return directFile;
    }

    return Array.from(clipboardData.items || [])
        .filter((item) => item.kind === "file")
        .map((item) => item.getAsFile())
        .find((file) => file?.type?.startsWith("image/")) || null;
}

function readAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => {
            const result = typeof reader.result === "string" ? reader.result : "";
            const commaIndex = result.indexOf(",");
            resolve(commaIndex >= 0 ? result.slice(commaIndex + 1) : result);
        };
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}

function createFileName(file) {
    if (file.name) {
        return file.name;
    }

    return `clipboard-product.${extensionFor(file.type)}`;
}

function extensionFor(contentType) {
    return contentType === "image/jpeg"
        ? "jpg"
        : contentType === "image/webp"
            ? "webp"
            : "png";
}
