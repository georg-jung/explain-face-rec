import "/image-compare-viewer-1.5.0/image-compare-viewer.min.js";

export function mount(id, showLabels, leftLabel, rightLabel) {
    const options = {
        addCircle: true,
        addCircleBlur: true,

        // Label Defaults
        showLabels: showLabels,
        labelOptions: {
            before: leftLabel,
            after: rightLabel,
            onHover: false
        },

        smoothing: false,
    };

    const element = document.getElementById(id);
    const viewer = new ImageCompare(element, options).mount();
}
