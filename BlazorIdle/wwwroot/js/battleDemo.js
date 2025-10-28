// 若用户滚动条接近底部（<= tolerance 像素），则自动滚动到底部。
// smooth: 是否平滑滚动（true/false）
export function scrollToBottomIfNear(el, tolerance = 12, smooth = false) {
    if (!el) return;
    try {
        const atOrNearBottom = (el.scrollTop + el.clientHeight) >= (el.scrollHeight - tolerance);
        if (atOrNearBottom) {
            const behavior = smooth ? 'smooth' : 'auto';
            if (typeof el.scrollTo === 'function') {
                el.scrollTo({ top: el.scrollHeight, behavior });
            } else {
                el.scrollTop = el.scrollHeight;
            }
        }
    } catch {
        // ignore
    }
}

// 兼容：无条件滚动到底部（保留原函数）
export function scrollToBottom(el, smooth = false) {
    if (!el) return;
    try {
        const behavior = smooth ? 'smooth' : 'auto';
        if (typeof el.scrollTo === 'function') {
            el.scrollTo({ top: el.scrollHeight, behavior });
        } else {
            el.scrollTop = el.scrollHeight;
        }
    } catch {
        // ignore
    }
}