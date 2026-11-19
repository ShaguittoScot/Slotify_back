(() => {
  'use strict';

  const DEFAULT_HOURS = [
    { dayOfWeek: 1, startTime: '09:00', endTime: '18:00' },
    { dayOfWeek: 2, startTime: '09:00', endTime: '18:00' },
    { dayOfWeek: 3, startTime: '09:00', endTime: '18:00' },
    { dayOfWeek: 4, startTime: '09:00', endTime: '18:00' },
    { dayOfWeek: 5, startTime: '09:00', endTime: '18:00' },
  ];

  const params = new URLSearchParams(window.location.search);
  const pathMatch = /\/book\/([^/?#]+)/i.exec(window.location.pathname);
  const initialSlug = (params.get('slug') || (pathMatch && decodeURIComponent(pathMatch[1])) || '').trim();

  const configuredApi = (params.get('api') || window.SLOTIFY_API_BASE || '').trim();
  const host = window.location.hostname || 'localhost';
  const isSameOriginPortal =
    /^\/book(?:\/|$)/i.test(window.location.pathname) ||
    window.location.port === '5272';
  const defaultApi = isSameOriginPortal
    ? `${window.location.origin}/api`
    : `http://${host}:5272/api`;
  const apiBase = (configuredApi || defaultApi).replace(/\/+$/, '');

  const state = {
    businesses: [],
    slug: initialSlug,
    business: null,
    selectedServiceIds: new Set(),
    selectedDate: '',
    selectedSlotIso: '',
    selectedSlotLabel: '',
    availability: null,
    customAnswers: {},
    submitting: false,
  };

  const els = {
    statusPill: document.getElementById('api-status-pill'),
    backToDirectoryBtn: document.getElementById('back-to-directory-btn'),
    loadingView: document.getElementById('loading-view'),
    errorView: document.getElementById('error-view'),
    errorTitle: document.getElementById('error-title'),
    errorMessage: document.getElementById('error-message'),
    retryBtn: document.getElementById('retry-btn'),
    errorDirectoryBtn: document.getElementById('error-directory-btn'),
    directoryView: document.getElementById('directory-view'),
    directorySearch: document.getElementById('directory-search'),
    directoryGrid: document.getElementById('directory-grid'),
    directoryEmpty: document.getElementById('directory-empty'),
    bookingView: document.getElementById('booking-view'),
    businessAvatar: document.getElementById('business-avatar'),
    businessSector: document.getElementById('business-sector'),
    businessTimezone: document.getElementById('business-timezone'),
    businessName: document.getElementById('business-name'),
    businessPhone: document.getElementById('business-phone'),
    formTitle: document.getElementById('form-title'),
    formDescription: document.getElementById('form-description'),
    servicesList: document.getElementById('services-list'),
    servicesSummaryChip: document.getElementById('services-summary-chip'),
    quickDays: document.getElementById('quick-days'),
    dateInput: document.getElementById('booking-date-input'),
    availabilityHint: document.getElementById('availability-hint'),
    slotsLoading: document.getElementById('slots-loading'),
    slotsEmpty: document.getElementById('slots-empty'),
    timeSlotsGrid: document.getElementById('time-slots-grid'),
    customFieldsSection: document.getElementById('custom-fields-section'),
    customFieldsContainer: document.getElementById('custom-fields-container'),
    contactHeading: document.getElementById('contact-section-heading'),
    bookingForm: document.getElementById('booking-form'),
    clientName: document.getElementById('client-name'),
    clientPhone: document.getElementById('client-phone'),
    clientEmail: document.getElementById('client-email'),
    clientNotes: document.getElementById('client-notes'),
    formErrorBanner: document.getElementById('form-error-banner'),
    checkoutSummaryText: document.getElementById('checkout-summary-text'),
    checkoutTotalPrice: document.getElementById('checkout-total-price'),
    submitBtn: document.getElementById('submit-booking-btn'),
    receiptView: document.getElementById('receipt-view'),
    receiptSubtitle: document.getElementById('receipt-subtitle'),
    receiptDetails: document.getElementById('receipt-details'),
    bookAnotherBtn: document.getElementById('book-another-btn'),
    receiptDirectoryBtn: document.getElementById('receipt-directory-btn'),
  };

  function showOnly(targetEl) {
    [els.loadingView, els.errorView, els.directoryView, els.bookingView, els.receiptView].forEach(
      (section) => section.classList.toggle('hidden', section !== targetEl)
    );
    els.backToDirectoryBtn.classList.toggle('hidden', targetEl === els.directoryView);
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function isValidHex(color) {
    return typeof color === 'string' && /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$/.test(color.trim());
  }

  function formatInitials(name) {
    const parts = String(name || 'SL').trim().split(/\s+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return (parts[0] || 'SL').slice(0, 2).toUpperCase();
  }

  function formatMoney(amount) {
    return `$${Number(amount || 0).toFixed(2)}`;
  }

  function getFormatter(timeZone) {
    return new Intl.DateTimeFormat('en-GB', {
      timeZone,
      calendar: 'gregory',
      numberingSystem: 'latn',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hourCycle: 'h23',
    });
  }

  function safeTimeZone(tz) {
    const candidate = tz || 'America/Mexico_City';
    try {
      getFormatter(candidate);
      return candidate;
    } catch {
      return 'America/Mexico_City';
    }
  }

  function toBusinessWallClockMs(utcMs, timeZone) {
    const parts = Object.fromEntries(
      getFormatter(timeZone).formatToParts(new Date(utcMs)).map((p) => [p.type, Number(p.value)])
    );
    return Date.UTC(
      parts.year,
      parts.month - 1,
      parts.day,
      parts.hour % 24,
      parts.minute,
      parts.second
    );
  }

  function wallClockToUtcDate(dateStr, hhmm, timeZone) {
    const [year, month, day] = dateStr.split('-').map(Number);
    const [hour, minute] = hhmm.slice(0, 5).split(':').map(Number);
    const wallMs = Date.UTC(year, month - 1, day, hour, minute, 0);
    const dayMs = 86_400_000;
    const offsets = new Set(
      [-dayMs, 0, dayMs].map((delta) => toBusinessWallClockMs(wallMs + delta, timeZone) - (wallMs + delta))
    );
    for (const offset of offsets) {
      const candidate = new Date(wallMs - offset);
      if (toBusinessWallClockMs(candidate.getTime(), timeZone) === wallMs) {
        return candidate;
      }
    }
    return null;
  }

  function todayIsoInZone(timeZone, addDays = 0) {
    const now = Date.now() + addDays * 86_400_000;
    const wall = new Date(toBusinessWallClockMs(now, timeZone));
    const y = wall.getUTCFullYear();
    const m = String(wall.getUTCMonth() + 1).padStart(2, '0');
    const d = String(wall.getUTCDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  function parseMinutes(hhmm) {
    const [h, m] = String(hhmm || '00:00').slice(0, 5).split(':').map(Number);
    return (Number.isFinite(h) ? h : 0) * 60 + (Number.isFinite(m) ? m : 0);
  }

  function formatMinutesToHHMM(totalMinutes) {
    const h = Math.floor(totalMinutes / 60);
    const m = totalMinutes % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  }

  function formatDateDisplay(dateStr, timeZone) {
    const utcMidday = wallClockToUtcDate(dateStr, '12:00', timeZone);
    if (!utcMidday) return dateStr;
    return utcMidday.toLocaleDateString('es-MX', {
      timeZone,
      weekday: 'long',
      day: 'numeric',
      month: 'long',
    });
  }

  function formatTimeDisplay(isoString, timeZone) {
    return new Date(isoString).toLocaleTimeString('es-MX', {
      timeZone,
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
  }

  async function apiFetch(path, options = {}) {
    const response = await fetch(`${apiBase}${path}`, {
      headers: {
        'Content-Type': 'application/json',
        ...(options.headers || {}),
      },
      ...options,
    });
    const text = await response.text();
    let json = null;
    try {
      json = text ? JSON.parse(text) : null;
    } catch {
      json = null;
    }
    if (!response.ok) {
      const msg =
        (json && (json.error || (Array.isArray(json.errors) && json.errors[0]) || json.message)) ||
        `Error HTTP ${response.status}`;
      throw new Error(msg);
    }
    return json;
  }

  function getSelectedSummary() {
    const services = state.business?.services || [];
    const chosen = services.filter((srv) => state.selectedServiceIds.has(srv.id));
    const totalPrice = chosen.reduce((sum, srv) => sum + Number(srv.price || 0), 0);
    const totalDuration =
      chosen.reduce((sum, srv) => sum + Number(srv.durationMinutes || 0), 0) || 30;
    return { chosen, totalPrice, totalDuration };
  }

  function applyTenantTheme(business) {
    const accent = isValidHex(business?.formConfig?.accentColor)
      ? business.formConfig.accentColor
      : isValidHex(business?.brandColor)
        ? business.brandColor
        : '#18181b';
    const secondary = isValidHex(business?.formConfig?.secondaryColor)
      ? business.formConfig.secondaryColor
      : accent;

    document.documentElement.style.setProperty('--accent', accent);
    document.documentElement.style.setProperty('--accent-secondary', secondary);
    document.documentElement.style.setProperty('--accent-soft', `${accent}1f`);
  }

  function updateCheckoutBar() {
    const { chosen, totalPrice, totalDuration } = getSelectedSummary();
    els.checkoutTotalPrice.textContent = formatMoney(totalPrice);
    els.servicesSummaryChip.textContent =
      chosen.length > 0
        ? `${chosen.length} seleccionado${chosen.length > 1 ? 's' : ''} · ${totalDuration} min`
        : 'Opcional';

    if (state.selectedSlotLabel && state.selectedDate) {
      const tz = safeTimeZone(state.business?.timeZone);
      els.checkoutSummaryText.textContent = `${formatDateDisplay(state.selectedDate, tz)} · ${state.selectedSlotLabel} (${totalDuration} min)`;
    } else {
      els.checkoutSummaryText.textContent = `Selecciona fecha y horario (${totalDuration} min)`;
    }
  }

  function renderDirectory(query = '') {
    const q = query.trim().toLowerCase();
    const filtered = state.businesses.filter((biz) => {
      if (!q) return true;
      const inName = String(biz.name || '').toLowerCase().includes(q);
      const inCat = String(biz.category || '').toLowerCase().includes(q);
      const inSrv = (biz.services || []).some((s) =>
        String(s.name || '').toLowerCase().includes(q)
      );
      return inName || inCat || inSrv;
    });

    els.directoryGrid.innerHTML = '';
    els.directoryEmpty.classList.toggle('hidden', filtered.length > 0);

    filtered.forEach((biz) => {
      const card = document.createElement('a');
      const targetUrl = new URL(window.location.href);
      targetUrl.searchParams.set('slug', biz.slug);
      card.href = targetUrl.toString();
      card.className = 'business-card';

      const accent = isValidHex(biz.brandColor) ? biz.brandColor : '#18181b';
      const avatarHtml = biz.logoUrl
        ? `<img src="${escapeHtml(biz.logoUrl)}" alt="${escapeHtml(biz.name)}" />`
        : escapeHtml(formatInitials(biz.name));

      const servicesCount = Array.isArray(biz.services) ? biz.services.length : 0;

      card.innerHTML = `
        <div class="card-head">
          <div class="card-avatar" style="background:${escapeHtml(accent)}">${avatarHtml}</div>
          <div>
            <h3 class="card-title">${escapeHtml(biz.name)}</h3>
            <p class="card-category">${escapeHtml(biz.category || 'Servicios')}</p>
          </div>
        </div>
        <p class="muted-text">${servicesCount} servicio${servicesCount === 1 ? '' : 's'} disponible${servicesCount === 1 ? '' : 's'}</p>
        <span class="card-cta">Reservar cita &rarr;</span>
      `;
      card.addEventListener('click', (event) => {
        event.preventDefault();
        navigateToSlug(biz.slug);
      });
      els.directoryGrid.appendChild(card);
    });
  }

  function renderServices() {
    const services = state.business?.services || [];
    els.servicesList.innerHTML = '';
    if (services.length === 0) {
      els.servicesList.innerHTML =
        '<p class="slots-message">Este negocio aún no tiene servicios publicados; puedes solicitar una cita general.</p>';
      return;
    }

    services.forEach((srv) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      const isSelected = state.selectedServiceIds.has(srv.id);
      btn.className = `service-option${isSelected ? ' selected' : ''}`;
      btn.innerHTML = `
        <span>
          <span class="service-title">${escapeHtml(srv.name)}</span>
          <span class="service-meta">${Number(srv.durationMinutes || 30)} min</span>
        </span>
        <span class="service-price">${formatMoney(srv.price)}</span>
      `;
      btn.addEventListener('click', () => {
        if (state.selectedServiceIds.has(srv.id)) {
          state.selectedServiceIds.delete(srv.id);
        } else {
          state.selectedServiceIds.add(srv.id);
        }
        renderServices();
        renderTimeSlots();
        updateCheckoutBar();
      });
      els.servicesList.appendChild(btn);
    });
  }

  function renderCustomFields() {
    const fields = state.business?.formConfig?.fields;
    if (!Array.isArray(fields) || fields.length === 0) {
      els.customFieldsSection.classList.add('hidden');
      els.contactHeading.textContent = '3. Tus datos de contacto';
      return;
    }

    const extra = fields.filter((f) => {
      const q = String(f.question || '').toLowerCase();
      return (
        f.type !== 'service_catalog' &&
        f.type !== 'date_time' &&
        !q.includes('nombre completo') &&
        !q.includes('teléfono') &&
        !q.includes('telefono') &&
        !q.includes('notas')
      );
    });

    if (extra.length === 0) {
      els.customFieldsSection.classList.add('hidden');
      els.contactHeading.textContent = '3. Tus datos de contacto';
      return;
    }

    els.customFieldsSection.classList.remove('hidden');
    els.contactHeading.textContent = '4. Tus datos de contacto';
    els.customFieldsContainer.innerHTML = '';

    extra.forEach((field) => {
      const wrapper = document.createElement('div');
      wrapper.className = 'field-block';
      const label = document.createElement('span');
      label.className = 'field-label';
      label.textContent = `${field.question}${field.required ? ' *' : ''}`;
      wrapper.appendChild(label);

      if (field.type === 'single_choice' && Array.isArray(field.options)) {
        const pills = document.createElement('div');
        pills.className = 'choice-pills';
        field.options.forEach((opt) => {
          const pill = document.createElement('button');
          pill.type = 'button';
          pill.className = `choice-pill${state.customAnswers[field.id] === opt ? ' selected' : ''}`;
          pill.textContent = opt;
          pill.addEventListener('click', () => {
            state.customAnswers[field.id] = opt;
            renderCustomFields();
          });
          pills.appendChild(pill);
        });
        wrapper.appendChild(pills);
      } else if (field.type === 'multiple_choice' && Array.isArray(field.options)) {
        const current = Array.isArray(state.customAnswers[field.id])
          ? state.customAnswers[field.id]
          : [];
        const pills = document.createElement('div');
        pills.className = 'choice-pills';
        field.options.forEach((opt) => {
          const pill = document.createElement('button');
          pill.type = 'button';
          pill.className = `choice-pill${current.includes(opt) ? ' selected' : ''}`;
          pill.textContent = opt;
          pill.addEventListener('click', () => {
            const next = current.includes(opt)
              ? current.filter((item) => item !== opt)
              : [...current, opt];
            state.customAnswers[field.id] = next;
            renderCustomFields();
          });
          pills.appendChild(pill);
        });
        wrapper.appendChild(pills);
      } else {
        const input = document.createElement(field.type === 'paragraph' ? 'textarea' : 'input');
        input.className = field.type === 'paragraph' ? 'text-input textarea-input' : 'text-input';
        input.placeholder = field.placeholder || 'Escribe tu respuesta...';
        input.value = state.customAnswers[field.id] || '';
        input.addEventListener('input', (e) => {
          state.customAnswers[field.id] = e.target.value;
        });
        wrapper.appendChild(input);
      }

      els.customFieldsContainer.appendChild(wrapper);
    });
  }

  function renderQuickDays(timeZone) {
    els.quickDays.innerHTML = '';
    for (let offset = 0; offset < 5; offset += 1) {
      const iso = todayIsoInZone(timeZone, offset);
      const chip = document.createElement('button');
      chip.type = 'button';
      chip.className = `day-chip${state.selectedDate === iso ? ' selected' : ''}`;
      if (offset === 0) chip.textContent = 'Hoy';
      else if (offset === 1) chip.textContent = 'Mañana';
      else chip.textContent = formatDateDisplay(iso, timeZone).split(',')[0];

      chip.addEventListener('click', () => selectDate(iso));
      els.quickDays.appendChild(chip);
    }
  }

  function renderTimeSlots() {
    els.timeSlotsGrid.innerHTML = '';
    els.slotsEmpty.classList.add('hidden');

    if (!state.availability || !state.business) return;

    const tz = safeTimeZone(state.availability.timeZone || state.business.timeZone);
    const { totalDuration } = getSelectedSummary();
    const minAdvanceMs = Number(state.business.minAdvanceHours || 0) * 3_600_000;
    const earliestAllowedMs = Date.now() + minAdvanceMs;

    const [y, m, d] = state.selectedDate.split('-').map(Number);
    const dayOfWeek = new Date(Date.UTC(y, m - 1, d)).getUTCDay();

    const allHours =
      Array.isArray(state.availability.hours) && state.availability.hours.length > 0
        ? state.availability.hours
        : DEFAULT_HOURS;

    const dayWindows = allHours.filter((h) => Number(h.dayOfWeek) === dayOfWeek);
    if (dayWindows.length === 0) {
      els.slotsEmpty.textContent = 'Cerrado en la fecha seleccionada. Elige otro día.';
      els.slotsEmpty.classList.remove('hidden');
      state.selectedSlotIso = '';
      state.selectedSlotLabel = '';
      updateCheckoutBar();
      return;
    }

    const occupied = (state.availability.occupied || []).map((interval) => ({
      start: new Date(interval.startTime).getTime(),
      end: new Date(interval.endTime).getTime(),
    }));

    const candidateSlots = [];
    dayWindows.forEach((win) => {
      const startMin = parseMinutes(win.startTime);
      const endMin = parseMinutes(win.endTime);
      for (let cursor = startMin; cursor + totalDuration <= endMin; cursor += 30) {
        const hhmm = formatMinutesToHHMM(cursor);
        const slotStart = wallClockToUtcDate(state.selectedDate, hhmm, tz);
        if (!slotStart) continue;
        const slotStartMs = slotStart.getTime();
        const slotEndMs = slotStartMs + totalDuration * 60_000;
        if (slotStartMs < earliestAllowedMs) continue;
        const overlaps = occupied.some((occ) => slotStartMs < occ.end && slotEndMs > occ.start);
        if (!overlaps) {
          candidateSlots.push({
            iso: slotStart.toISOString(),
            label: formatTimeDisplay(slotStart.toISOString(), tz),
          });
        }
      }
    });

    if (candidateSlots.length === 0) {
      els.slotsEmpty.textContent =
        'No quedan horarios disponibles para esta fecha o duración. Prueba con otro día.';
      els.slotsEmpty.classList.remove('hidden');
      state.selectedSlotIso = '';
      state.selectedSlotLabel = '';
      updateCheckoutBar();
      return;
    }

    if (!candidateSlots.some((s) => s.iso === state.selectedSlotIso)) {
      state.selectedSlotIso = candidateSlots[0].iso;
      state.selectedSlotLabel = candidateSlots[0].label;
    }

    candidateSlots.forEach((slot) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = `slot-btn${state.selectedSlotIso === slot.iso ? ' selected' : ''}`;
      btn.textContent = slot.label;
      btn.addEventListener('click', () => {
        state.selectedSlotIso = slot.iso;
        state.selectedSlotLabel = slot.label;
        renderTimeSlots();
        updateCheckoutBar();
      });
      els.timeSlotsGrid.appendChild(btn);
    });

    updateCheckoutBar();
  }

  async function loadAvailability(dateStr) {
    if (!state.business) return;
    els.slotsLoading.classList.remove('hidden');
    els.slotsEmpty.classList.add('hidden');
    els.timeSlotsGrid.innerHTML = '';

    try {
      const res = await apiFetch(
        `/public/businesses/${encodeURIComponent(state.business.slug)}/availability?date=${encodeURIComponent(dateStr)}`
      );
      state.availability = res?.data || null;
      renderTimeSlots();
    } catch (error) {
      els.slotsEmpty.textContent = error.message || 'No pudimos consultar la disponibilidad.';
      els.slotsEmpty.classList.remove('hidden');
    } finally {
      els.slotsLoading.classList.add('hidden');
    }
  }

  function selectDate(dateStr) {
    state.selectedDate = dateStr;
    state.selectedSlotIso = '';
    state.selectedSlotLabel = '';
    els.dateInput.value = dateStr;
    renderQuickDays(safeTimeZone(state.business?.timeZone));
    void loadAvailability(dateStr);
  }

  async function openBusinessBooking(slug) {
    state.slug = slug;
    state.selectedServiceIds = new Set();
    state.customAnswers = {};
    els.formErrorBanner.classList.add('hidden');
    showOnly(els.loadingView);

    try {
      const res = await apiFetch(`/public/businesses/${encodeURIComponent(slug)}`);
      const biz = res?.data;
      if (!biz || !biz.id) {
        throw new Error('No encontramos la configuración de este establecimiento.');
      }
      state.business = biz;
      applyTenantTheme(biz);

      const tz = safeTimeZone(biz.timeZone);
      const minDate = todayIsoInZone(tz, 0);
      const maxDate = todayIsoInZone(tz, Math.max(1, Number(biz.maxAdvanceDays || 30)));
      els.dateInput.min = minDate;
      els.dateInput.max = maxDate;

      els.businessAvatar.innerHTML = biz.logoUrl
        ? `<img src="${escapeHtml(biz.logoUrl)}" alt="${escapeHtml(biz.name)}" />`
        : escapeHtml(formatInitials(biz.name));
      els.businessSector.textContent = biz.sector || 'RESERVAS EN LÍNEA';
      els.businessTimezone.textContent = tz.replace(/_/g, ' ');
      els.businessName.textContent = biz.name;
      els.businessPhone.textContent = biz.phone || '';
      els.availabilityHint.textContent = `Zona horaria: ${tz.replace(/_/g, ' ')}`;

      els.formTitle.textContent = biz.formConfig?.title || 'Reserva tu cita en línea';
      els.formDescription.textContent =
        biz.formConfig?.description ||
        `Elige servicio, fecha y horario disponible en ${biz.name}.`;

      document.title = `${biz.name} — Reservar en Slotify`;

      renderServices();
      renderCustomFields();
      showOnly(els.bookingView);
      selectDate(minDate);
    } catch (error) {
      els.errorTitle.textContent = 'No pudimos cargar el formulario';
      els.errorMessage.textContent = error.message;
      showOnly(els.errorView);
    }
  }

  async function openDirectory() {
    state.slug = '';
    applyTenantTheme(null);
    document.title = 'Slotify — Reservas en Línea';
    showOnly(els.loadingView);

    try {
      const payload = await apiFetch('/businesses');
      const list = Array.isArray(payload) ? payload : Array.isArray(payload?.data) ? payload.data : [];
      state.businesses = list;
      els.statusPill.textContent = 'En línea';
      renderDirectory(els.directorySearch.value);
      showOnly(els.directoryView);
    } catch (error) {
      els.statusPill.textContent = 'Sin conexión';
      els.errorTitle.textContent = 'No pudimos conectar con Slotify';
      els.errorMessage.textContent = error.message;
      showOnly(els.errorView);
    }
  }

  function navigateToSlug(slug) {
    const nextUrl = new URL(window.location.href);
    if (/^\/book(?:\/|$)/i.test(nextUrl.pathname)) {
      nextUrl.pathname = slug ? `/book/${encodeURIComponent(slug)}` : '/book/';
      nextUrl.searchParams.delete('slug');
    } else if (slug) {
      nextUrl.searchParams.set('slug', slug);
    } else {
      nextUrl.searchParams.delete('slug');
    }
    window.history.pushState({ slug }, '', nextUrl.toString());

    if (slug) void openBusinessBooking(slug);
    else void openDirectory();
  }

  function buildNotesPayload() {
    const lines = [];
    const rawNotes = els.clientNotes.value.trim();
    if (rawNotes) lines.push(rawNotes);

    const fields = state.business?.formConfig?.fields || [];
    fields.forEach((field) => {
      const val = state.customAnswers[field.id];
      if (val !== undefined && val !== null && String(val).trim() !== '') {
        const formatted = Array.isArray(val) ? val.join(', ') : String(val);
        lines.push(`${field.question}: ${formatted}`);
      }
    });
    return lines.join('\n').trim();
  }

  els.bookingForm.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (state.submitting || !state.business) return;
    els.formErrorBanner.classList.add('hidden');

    const clientName = els.clientName.value.trim();
    const clientPhone = els.clientPhone.value.trim();
    const clientEmail = els.clientEmail.value.trim();
    const { chosen, totalPrice, totalDuration } = getSelectedSummary();

    if (!clientName) {
      els.formErrorBanner.textContent = 'Por favor ingresa tu nombre completo.';
      els.formErrorBanner.classList.remove('hidden');
      els.clientName.focus();
      return;
    }

    if (!state.selectedSlotIso) {
      els.formErrorBanner.textContent = 'Selecciona una fecha y horario disponible.';
      els.formErrorBanner.classList.remove('hidden');
      return;
    }

    const startDate = new Date(state.selectedSlotIso);
    const endDate = new Date(startDate.getTime() + totalDuration * 60_000);
    const customNotes = buildNotesPayload();
    const servicesLine = chosen.length > 0 ? `Servicios: ${chosen.map((s) => s.name).join(', ')}` : '';
    const combinedNotes = [servicesLine, customNotes].filter(Boolean).join('\n');

    state.submitting = true;
    els.submitBtn.disabled = true;
    els.submitBtn.textContent = 'Confirmando reserva...';

    try {
      const res = await apiFetch(
        `/public/businesses/${encodeURIComponent(state.business.slug)}/appointments`,
        {
          method: 'POST',
          body: JSON.stringify({
            startTime: startDate.toISOString(),
            endTime: endDate.toISOString(),
            durationMinutes: totalDuration,
            clientName,
            clientPhone: clientPhone || undefined,
            clientEmail: clientEmail || undefined,
            agreedTotal: totalPrice,
            notes: combinedNotes || undefined,
          }),
        }
      );

      const created = res?.data;
      if (!created || !created.id) {
        throw new Error('El servidor no confirmó la creación de la cita.');
      }

      const tz = safeTimeZone(state.business.timeZone);
      els.receiptSubtitle.textContent = `Tu cita en ${created.businessName || state.business.name} quedó registrada.`;
      const rows = [
        ['Cliente', created.clientName || clientName],
        ['Fecha', formatDateDisplay(state.selectedDate, tz)],
        ['Horario', formatTimeDisplay(created.startTime, tz)],
        [
          'Servicios',
          chosen.length > 0 ? chosen.map((s) => s.name).join(', ') : 'Cita general',
        ],
        ['Total estimado', formatMoney(created.agreedTotal ?? totalPrice)],
        ['Estado', created.status === 'pending' ? 'Pendiente' : 'Confirmada'],
      ];
      if (created.cancellationToken) {
        rows.push(['Código de referencia', created.cancellationToken]);
      }

      els.receiptDetails.innerHTML = rows
        .map(
          ([label, val]) => `
            <div class="receipt-row">
              <dt>${escapeHtml(label)}</dt>
              <dd>${escapeHtml(val)}</dd>
            </div>
          `
        )
        .join('');

      showOnly(els.receiptView);
    } catch (error) {
      els.formErrorBanner.textContent = error.message || 'No se pudo completar la reserva.';
      els.formErrorBanner.classList.remove('hidden');
    } finally {
      state.submitting = false;
      els.submitBtn.disabled = false;
      els.submitBtn.textContent = 'Confirmar y Agendar Cita';
    }
  });

  els.dateInput.addEventListener('change', (e) => {
    if (e.target.value) selectDate(e.target.value);
  });

  els.directorySearch.addEventListener('input', (e) => {
    renderDirectory(e.target.value);
  });

  els.backToDirectoryBtn.addEventListener('click', () => navigateToSlug(''));
  els.errorDirectoryBtn.addEventListener('click', () => navigateToSlug(''));
  els.receiptDirectoryBtn.addEventListener('click', () => navigateToSlug(''));
  document.getElementById('brand-home-link').addEventListener('click', (e) => {
    e.preventDefault();
    navigateToSlug('');
  });
  els.bookAnotherBtn.addEventListener('click', () => {
    if (state.business?.slug) void openBusinessBooking(state.business.slug);
  });
  els.retryBtn.addEventListener('click', () => {
    if (state.slug) void openBusinessBooking(state.slug);
    else void openDirectory();
  });

  window.addEventListener('popstate', () => {
    const nextParams = new URLSearchParams(window.location.search);
    const nextSlug = (nextParams.get('slug') || '').trim();
    if (nextSlug) void openBusinessBooking(nextSlug);
    else void openDirectory();
  });

  if (initialSlug) {
    els.statusPill.textContent = 'En línea';
    void openBusinessBooking(initialSlug);
  } else {
    void openDirectory();
  }
})();
