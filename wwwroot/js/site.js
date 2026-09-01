// ============================================
// Abdurrahman Tatlıcı - Araç Görev Takip Sistemi
// Site JavaScript
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    // ---------- Global DataTables Ayarları ----------
    if (typeof $.fn.dataTable !== 'undefined') {
        $.extend(true, $.fn.dataTable.defaults, {
            language: {
                "emptyTable": "Tabloda herhangi bir veri mevcut değil",
                "info": "_TOTAL_ kayıttan _START_ - _END_ arasındaki kayıtlar gösteriliyor",
                "infoEmpty": "Kayıt yok",
                "infoFiltered": "(_MAX_ kayıt içerisinden bulunan)",
                "lengthMenu": "Sayfada _MENU_ kayıt göster",
                "loadingRecords": "Yükleniyor...",
                "processing": "İşleniyor...",
                "search": "Ara:",
                "zeroRecords": "Eşleşen kayıt bulunamadı",
                "paginate": {
                    "first": "İlk",
                    "last": "Son",
                    "next": "Sonraki",
                    "previous": "Önceki"
                },
                "aria": {
                    "sortAscending": ": artan sütun sıralamasını aktifleştir",
                    "sortDescending": ": azalan sütun sıralamasını aktifleştir"
                }
            }
        });
    }

    // ---------- Telefon Numarası Maskeleme ----------
    const telefonInputlar = document.querySelectorAll('input[name="KullananTelefon"], input[data-mask="phone"]');
    telefonInputlar.forEach(function (input) {
        input.setAttribute('maxlength', '14');
        input.setAttribute('inputmode', 'tel');

        input.addEventListener('input', function (e) {
            let val = e.target.value.replace(/\D/g, '');

            // Başına 0 ekle eğer yoksa ve 5 ile başlıyorsa
            if (val.length > 0 && val[0] === '5') {
                val = '0' + val;
            }

            // Format: 05XX XXX XX XX
            let formatted = '';
            if (val.length > 0) formatted += val.substring(0, 4);
            if (val.length > 4) formatted += ' ' + val.substring(4, 7);
            if (val.length > 7) formatted += ' ' + val.substring(7, 9);
            if (val.length > 9) formatted += ' ' + val.substring(9, 11);

            e.target.value = formatted;
        });
    });

    // ---------- Sorgulama Sekmeleri ----------
    const queryTabs = document.querySelectorAll('.query-tab');
    const queryPanels = document.querySelectorAll('.query-panel');
    const sorgulamaTipiInput = document.getElementById('SorgulamaTipi');

    queryTabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            const target = this.getAttribute('data-target');

            // Sekmeleri güncelle
            queryTabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');

            // Panelleri güncelle
            queryPanels.forEach(p => p.classList.remove('active'));
            const targetPanel = document.getElementById(target);
            if (targetPanel) targetPanel.classList.add('active');

            // Hidden input güncelle
            if (sorgulamaTipiInput) {
                sorgulamaTipiInput.value = target === 'panel-kod' ? 'kod' : 'isim';
            }
        });
    });

    // ---------- İstatistik Sayaç Animasyonu ----------
    const statNumbers = document.querySelectorAll('.stat-card h2');
    const observerOptions = { threshold: 0.5 };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                const el = entry.target;
                const finalValue = parseInt(el.textContent);

                if (isNaN(finalValue) || finalValue === 0) return;

                let current = 0;
                const duration = 800; // ms
                const step = Math.max(1, Math.floor(finalValue / (duration / 16)));
                const startTime = performance.now();

                function animate(currentTime) {
                    const elapsed = currentTime - startTime;
                    const progress = Math.min(elapsed / duration, 1);
                    // Ease-out quad
                    const eased = 1 - (1 - progress) * (1 - progress);
                    current = Math.floor(eased * finalValue);
                    el.textContent = current;

                    if (progress < 1) {
                        requestAnimationFrame(animate);
                    } else {
                        el.textContent = finalValue;
                    }
                }

                el.textContent = '0';
                requestAnimationFrame(animate);
                observer.unobserve(el);
            }
        });
    }, observerOptions);

    statNumbers.forEach(function (el) {
        observer.observe(el);
    });

    // ---------- Alert Otomatik Kapanma ----------
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const closeBtn = alert.querySelector('.btn-close');
            if (closeBtn) closeBtn.click();
        }, 5000);
    });

    // ---------- Tarih/Saat Input - Saniye Kaldır ----------
    const datetimeInputs = document.querySelectorAll('input[type="datetime-local"]');
    datetimeInputs.forEach(function (input) {
        input.setAttribute('step', '60');
    });

    // ---------- Form Validasyon Geri Bildirimi ----------
    const forms = document.querySelectorAll('form');
    forms.forEach(function (form) {
        form.addEventListener('submit', function (e) {
            const requiredInputs = form.querySelectorAll('[required], [data-val-required]');
            let hasError = false;

            requiredInputs.forEach(function (input) {
                if (!input.value || input.value.trim() === '') {
                    input.classList.add('is-invalid');
                    hasError = true;
                } else {
                    input.classList.remove('is-invalid');
                }
            });

            // Telefon özel validasyonu
            const telefonInput = form.querySelector('input[name="KullananTelefon"]');
            if (telefonInput && telefonInput.value) {
                const digits = telefonInput.value.replace(/\D/g, '');
                if (digits.length < 10 || digits.length > 11) {
                    telefonInput.classList.add('is-invalid');
                    hasError = true;
                }
            }
        });
    });

    // ---------- Aktif Navbar Link Vurgulama ----------
    const currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.app-navbar .nav-link').forEach(function (link) {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase()) && href !== '/') {
            link.classList.add('active');
        }
    });

    // ---------- Buton Ripple Efekti ----------
    document.querySelectorAll('.btn').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = btn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${x}px;
                top: ${y}px;
                background: rgba(255,255,255,0.3);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple-animation 0.6s ease-out;
                pointer-events: none;
            `;

            btn.style.position = 'relative';
            btn.style.overflow = 'hidden';
            btn.appendChild(ripple);

            setTimeout(() => ripple.remove(), 600);
        });
    });

    // Ripple animation keyframe
    const style = document.createElement('style');
    style.textContent = `
        @keyframes ripple-animation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);

    // ---------- Global Modal Fix ----------
    // Move all modals to the end of the body to prevent z-index/backdrop issues
    const modals = document.querySelectorAll('.modal');
    modals.forEach(function (modal) {
        document.body.appendChild(modal);
    });

    // ---------- Bootstrap Tooltip Initialization ----------
    // Initialize all tooltips on the page
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"], [title]:not([title=""])'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        // Skip elements that already have tooltip or are DataTables buttons (they handle their own titles sometimes, but safe to apply if they have title)
        if (!tooltipTriggerEl.hasAttribute('data-bs-original-title')) {
             return new bootstrap.Tooltip(tooltipTriggerEl, {
                 boundary: document.body
             });
        }
    });
});
