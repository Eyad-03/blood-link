/* =====================================================================
   BloodLink — Donate page (/Donor/Donate)
   The donor fills in blood type, phone and location, then the offer is saved:
     PUT /api/profile/donor-offer   (Controllers/Api/ProfileController.cs)
   After that the donor is "available" and shows on the /Donor/Search map.
   ===================================================================== */
document.addEventListener("DOMContentLoaded", () => {
    const $ = id => document.getElementById(id);
    Api.requireLogin(["Donor"]);

    const form = $("donorForm");
    const status = $("mapStatus");
    let map = null, marker = null;
    let position = { lat: 32.5556, lng: 35.8500 };   // Irbid until the donor picks a place

    /* ---------- Map ---------- */
    if ($("locationMap") && typeof L !== "undefined") {
        map = L.map($("locationMap")).setView([position.lat, position.lng], 13);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        marker = L.marker([position.lat, position.lng], { draggable: true }).addTo(map);
        marker.bindPopup("Your selected donation location");
        marker.on("dragend", () => { position = marker.getLatLng(); });

        if (status) { status.textContent = "Map loaded successfully."; status.classList.remove("error"); }
        setTimeout(() => map.invalidateSize(), 300);
    } else if (status) {
        status.textContent = "Map could not load. Please check the Leaflet library.";
    }

    // "Use my location" button → browser GPS.
    $("useLocation")?.addEventListener("click", () => {
        if (!navigator.geolocation) { alert("Location is not supported by this browser."); return; }
        navigator.geolocation.getCurrentPosition(p => {
            position = { lat: p.coords.latitude, lng: p.coords.longitude };
            if (marker) { marker.setLatLng(position); map.setView(position, 15); }
            const precise = form.querySelector('input[name="locationMode"][value="precise"]');
            if (precise) { precise.checked = true; precise.dispatchEvent(new Event("change", { bubbles: true })); }
        }, () => alert("We couldn't get your location. You can move the marker instead."));
    });

    // Highlight the chosen location option and update the label.
    form.addEventListener("change", e => {
        if (e.target.name !== "locationMode") return;
        form.querySelectorAll(".location-option").forEach(o => o.classList.toggle("selected", o.querySelector("input").checked));
        $("locationLabel").textContent = e.target.value === "precise" ? "Exact location · marker on the map" : "Approximate location · " + $("city").value;
    });

    /* ---------- Fill the form with what we already know ---------- */
    Api.get("/api/profile").then(p => {
        if (p.name) $("fullName").value = p.name;
        if (p.bloodType) $("bloodType").value = p.bloodType;
        if (p.phone) $("phone").value = p.phone;
        if (p.location) $("city").value = p.location;
    }).catch(() => { });

    /* ---------- Submit ---------- */
    form.addEventListener("submit", async e => {
        e.preventDefault();
        if (!form.reportValidity()) return;

        const precise = form.querySelector('input[name="locationMode"]:checked')?.value === "precise";
        const offer = {
            fullName: $("fullName").value.trim(),
            bloodType: $("bloodType").value,
            phone: $("phone").value.trim(),
            city: $("city").value.trim(),
            contactMethod: $("contactMethod").value,
            shareExactLocation: precise,
            latitude: precise ? position.lat : null,
            longitude: precise ? position.lng : null
        };

        try {
            await Api.put("/api/profile/donor-offer", offer);
        } catch (err) {
            alert(err.message);
            return;
        }

        $("successSummary").innerHTML =
            `<p><b>${offer.bloodType}</b> · ${offer.city}</p><p>${$("availability").selectedOptions[0].text}</p>`;
        $("successModal").hidden = false;
    });

    $("closeSuccess")?.addEventListener("click", () => { $("successModal").hidden = true; window.location.href = "/Donor/Requests"; });
    $("cancelButton")?.addEventListener("click", () => { window.location.href = "/"; });
});
