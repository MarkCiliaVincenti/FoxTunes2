(function () {
    var parts = [];
    if (tag.disccount != 1 && tag.disc) {
        parts.push(tag.disc);
    }
    if (tag.track) {
        parts.push(zeropad(tag.track, 2));
    }
    return parts.join(" - ");
})()