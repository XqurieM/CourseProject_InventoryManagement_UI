document.addEventListener('DOMContentLoaded', function () {
  const toggle = document.getElementById('themeToggle');
  if (toggle) {
    toggle.addEventListener('click', function (e) {
      e.preventDefault();
      const html = document.documentElement;
      const dark = html.getAttribute('data-bs-theme') === 'dark';
      html.setAttribute('data-bs-theme', dark ? 'light' : 'dark');
      toggle.innerHTML = dark ? '<i class="fa-solid fa-moon"></i>' : '<i class="fa-solid fa-sun"></i>';
    });
  }
  document.querySelectorAll('[data-sortable="true"]').forEach(function (el) {
    if (window.Sortable) Sortable.create(el, { handle: '.rule-handle, .field-handle', animation: 150 });
  });

  const bulkFieldContainer = document.getElementById('bulkFieldRows');
  const bulkFieldTemplate = document.getElementById('bulkFieldRowTemplate');
  const addFieldRowButton = document.getElementById('addFieldRowButton');
  const bulkItemContainer = document.getElementById('bulkItemRows');
  const bulkItemTemplate = document.getElementById('bulkItemRowTemplate');
  const addItemRowButton = document.getElementById('addItemRowButton');
  const bulkCustomIdContainer = document.getElementById('bulkCustomIdRows');
  const bulkCustomIdTemplate = document.getElementById('bulkCustomIdRowTemplate');
  const addCustomIdRowButton = document.getElementById('addCustomIdRowButton');
  const existingCustomIdRows = Array.from(document.querySelectorAll('[data-customid-static-row]'));
	  const inventoryImageUrlInput = document.getElementById('inventoryImageUrlInput');
	  const inventoryImageFileInput = document.getElementById('inventoryImageFileInput');
	  const inventoryPreviewImage = document.getElementById('inventoryCreatePreviewImage');
	  const inventoryPreviewPlaceholder = document.getElementById('inventoryCreatePreviewPlaceholder');
	  const accessManager = document.querySelector('[data-access-manager]');
	  let activeCustomIdRow = null;

  function reindexBulkFieldRows() {
    if (!bulkFieldContainer) return;
    const rows = Array.from(bulkFieldContainer.querySelectorAll('.bulk-field-row'));
    rows.forEach(function (row, index) {
      row.querySelectorAll('input, select, textarea').forEach(function (input) {
        if (!input.name) return;
        input.name = input.name.replace(/FieldForms\[\d+\]/g, 'FieldForms[' + index + ']');
      });

      const orderInput = row.querySelector('.field-order-input');
      if (orderInput && !orderInput.value) {
        orderInput.value = String(index + 1);
      }
    });
  }

  if (addFieldRowButton && bulkFieldContainer && bulkFieldTemplate) {
    addFieldRowButton.addEventListener('click', function () {
      const nextIndex = bulkFieldContainer.querySelectorAll('.bulk-field-row').length;
      const nextOrderStart = Number(bulkFieldContainer.dataset.nextOrderStart || '1');
      const nextOrder = nextOrderStart + nextIndex;
      const html = bulkFieldTemplate.innerHTML
        .replace(/__index__/g, String(nextIndex))
        .replace(/__order__/g, String(nextOrder));
      bulkFieldContainer.insertAdjacentHTML('beforeend', html);
      reindexBulkFieldRows();
    });

    bulkFieldContainer.addEventListener('click', function (event) {
      const button = event.target.closest('.remove-field-row');
      if (!button) return;
      const row = button.closest('.bulk-field-row');
      if (!row) return;
      row.remove();
      reindexBulkFieldRows();
    });
  }

  function reindexBulkItemRows() {
    if (!bulkItemContainer) return;
    const rows = Array.from(bulkItemContainer.querySelectorAll('.bulk-item-row'));
    rows.forEach(function (row, index) {
      row.querySelectorAll('input, select, textarea').forEach(function (input) {
        if (!input.name) return;
        input.name = input.name.replace(/ItemRows\[\d+\]/g, 'ItemRows[' + index + ']');
      });
    });
  }

  if (addItemRowButton && bulkItemContainer && bulkItemTemplate) {
    addItemRowButton.addEventListener('click', function () {
      const nextIndex = bulkItemContainer.querySelectorAll('.bulk-item-row').length;
      const html = bulkItemTemplate.innerHTML.replace(/__index__/g, String(nextIndex));
      bulkItemContainer.insertAdjacentHTML('beforeend', html);
      reindexBulkItemRows();
    });

    bulkItemContainer.addEventListener('click', function (event) {
      const button = event.target.closest('.remove-item-row');
      if (!button) return;
      const row = button.closest('.bulk-item-row');
      if (!row) return;
      row.remove();
      reindexBulkItemRows();
    });
  }

  function reindexBulkCustomIdRows() {
    if (!bulkCustomIdContainer) return;
    const rows = Array.from(bulkCustomIdContainer.querySelectorAll('.bulk-customid-row'));
    const nextOrderStart = Number(bulkCustomIdContainer.dataset.nextOrderStart || '1');
    rows.forEach(function (row, index) {
      row.querySelectorAll('input, select').forEach(function (input) {
        if (!input.name) return;
        input.name = input.name.replace(/RuleForms\[\d+\]/g, 'RuleForms[' + index + ']');
      });

      const orderInput = row.querySelector('.customid-order-input');
      if (orderInput && !orderInput.value) {
        orderInput.value = String(nextOrderStart + index);
      }
    });
  }

  function padNumber(value, size) {
    return String(value).padStart(size, '0');
  }

  function formatDatePreview(pattern) {
    const now = new Date();
    const replacements = {
      yyyy: String(now.getUTCFullYear()),
      yy: String(now.getUTCFullYear()).slice(-2),
      MM: padNumber(now.getUTCMonth() + 1, 2),
      dd: padNumber(now.getUTCDate(), 2),
      HH: padNumber(now.getUTCHours(), 2),
      mm: padNumber(now.getUTCMinutes(), 2),
      ss: padNumber(now.getUTCSeconds(), 2)
    };

    let output = pattern || 'yyyyMMdd';
    Object.keys(replacements).forEach(function (key) {
      output = output.replaceAll(key, replacements[key]);
    });

    return output;
  }

  function formatSequencePreview(pattern) {
    const baseNumber = 42;
    if (!pattern) return '042';

    const match = /^D(\d+)$/i.exec(pattern.trim());
    if (match) {
      return padNumber(baseNumber, Number(match[1]));
    }

    return String(baseNumber);
  }

  function formatGuidPreview(pattern) {
    const guid = '550e8400-e29b-41d4-a716-446655440000';
    const normalized = (pattern || 'D').trim().toUpperCase();

    if (normalized === 'N') return guid.replaceAll('-', '');
    if (normalized === 'B') return `{${guid}}`;
    if (normalized === 'P') return `(${guid})`;
    return guid;
  }

  function getCustomIdPreview(typeValue, formatValue, staticTextValue) {
    const type = Number(typeValue || 1);
    const format = (formatValue || '').trim();
    const staticText = staticTextValue || '';

    switch (type) {
      case 1:
        return {
          value: staticText || 'BOOK-',
          hint: 'Static text values are appended exactly as written.'
        };
      case 2:
        return {
          value: '3fa9c',
          hint: 'Random20Bit produces a short random hexadecimal sample.'
        };
      case 3:
        return {
          value: '3fa9c17b',
          hint: 'Random32Bit produces a longer random hexadecimal sample.'
        };
      case 4:
        return {
          value: '482913',
          hint: 'Random6Digit produces a six-digit numeric sample.'
        };
      case 5:
        return {
          value: '482913640',
          hint: 'Random9Digit produces a nine-digit numeric sample.'
        };
      case 6:
        return {
          value: formatGuidPreview(format),
          hint: 'Guid preview respects common formats like D, N, B and P.'
        };
      case 7:
        return {
          value: formatDatePreview(format),
          hint: 'DateTime preview uses the current UTC time and your format pattern.'
        };
      case 8:
        return {
          value: formatSequencePreview(format),
          hint: 'Sequence preview uses sample counter 42 and respects D-format padding.'
        };
      default:
        return {
          value: staticText || 'BOOK-',
          hint: 'Unknown type, falling back to static text preview.'
        };
    }
  }

  function getCustomIdRows() {
    if (!bulkCustomIdContainer) return [];
    return Array.from(bulkCustomIdContainer.querySelectorAll('.bulk-customid-row'));
  }

  function getAllCustomIdRows() {
    return existingCustomIdRows.concat(getCustomIdRows());
  }

  function syncCustomIdRowState(row) {
    if (!row) return;

    const typeInput = row.querySelector('.customid-type-input');
    const formatInput = row.querySelector('.customid-format-input');
    const staticInput = row.querySelector('.customid-static-input');

    if (!typeInput || !formatInput || !staticInput) return;

    const type = Number(typeInput.value || 1);
    const usesStaticText = type === 1;
    const usesFormat = type === 6 || type === 7 || type === 8;

    staticInput.disabled = !usesStaticText;
    if (!usesStaticText) {
      staticInput.value = '';
      staticInput.placeholder = 'Only used for StaticText';
    } else {
      staticInput.placeholder = 'BOOK-';
    }

    formatInput.disabled = !usesFormat;
    if (!usesFormat) {
      formatInput.value = '';
      formatInput.placeholder = 'Only used for Guid / DateTime / Sequence';
    } else if (type === 6) {
      formatInput.placeholder = 'D, N, B, P';
    } else if (type === 7) {
      formatInput.placeholder = 'yyyyMMdd, yyyy, ddMMyy';
    } else if (type === 8) {
      formatInput.placeholder = 'D3, D6';
    }
  }

  function resolveActiveCustomIdRow() {
    const rows = getAllCustomIdRows();
    if (rows.length === 0) return null;
    if (activeCustomIdRow && rows.includes(activeCustomIdRow)) return activeCustomIdRow;
    return rows[0];
  }

  function updateCustomIdPreview() {
    const currentRow = resolveActiveCustomIdRow();
    if (!currentRow) return;

    const previewValue = document.getElementById('customIdPreviewValue');
    const previewCombined = document.getElementById('customIdPreviewCombined');
    const previewType = document.getElementById('customIdPreviewType');
    const previewOrder = document.getElementById('customIdPreviewOrder');
    const previewFormat = document.getElementById('customIdPreviewFormat');
    const previewHint = document.getElementById('customIdPreviewHint');

    if (!previewValue || !previewCombined || !previewType || !previewOrder || !previewFormat || !previewHint) return;

    const typeInput = currentRow.querySelector('.customid-type-input');
    const orderInput = currentRow.querySelector('.customid-order-input');
    const formatInput = currentRow.querySelector('.customid-format-input');
    const staticInput = currentRow.querySelector('.customid-static-input');

    if (!typeInput) return;

    const selectedOption = typeInput.options[typeInput.selectedIndex];
    const preview = getCustomIdPreview(
      typeInput.value,
      formatInput ? formatInput.value : '',
      staticInput ? staticInput.value : ''
    );

    previewType.textContent = selectedOption ? selectedOption.text : 'Rule';
    previewValue.textContent = preview.value;
    previewOrder.textContent = orderInput && orderInput.value ? orderInput.value : '1';
    previewFormat.textContent = formatInput && formatInput.value ? formatInput.value : '-';
    previewHint.textContent = preview.hint;

    const combined = getAllCustomIdRows()
      .map(function (row) {
        syncCustomIdRowState(row);
        const rowType = row.querySelector('.customid-type-input');
        const rowFormat = row.querySelector('.customid-format-input');
        const rowStatic = row.querySelector('.customid-static-input');
        if (!rowType) return '';
        return getCustomIdPreview(
          rowType.value,
          rowFormat ? rowFormat.value : '',
          rowStatic ? rowStatic.value : ''
        ).value;
      })
      .join('');

    previewCombined.textContent = combined || 'INV-2026001';
  }

  if (addCustomIdRowButton && bulkCustomIdContainer && bulkCustomIdTemplate) {
    addCustomIdRowButton.addEventListener('click', function () {
      const nextIndex = bulkCustomIdContainer.querySelectorAll('.bulk-customid-row').length;
      const nextOrderStart = Number(bulkCustomIdContainer.dataset.nextOrderStart || '1');
      const nextOrder = nextOrderStart + nextIndex;
      const html = bulkCustomIdTemplate.innerHTML
        .replace(/__index__/g, String(nextIndex))
        .replace(/__order__/g, String(nextOrder));
      bulkCustomIdContainer.insertAdjacentHTML('beforeend', html);
      reindexBulkCustomIdRows();
      activeCustomIdRow = getCustomIdRows().at(-1) || null;
      syncCustomIdRowState(activeCustomIdRow);
      updateCustomIdPreview();
    });

    bulkCustomIdContainer.addEventListener('click', function (event) {
      const removeButton = event.target.closest('.remove-customid-row');
      if (removeButton) {
        const row = removeButton.closest('.bulk-customid-row');
        if (!row) return;
        row.remove();
        activeCustomIdRow = null;
        reindexBulkCustomIdRows();
        updateCustomIdPreview();
        return;
      }

      const row = event.target.closest('.bulk-customid-row');
      if (row) {
        activeCustomIdRow = row;
        updateCustomIdPreview();
      }
    });

    bulkCustomIdContainer.addEventListener('input', function (event) {
      const row = event.target.closest('.bulk-customid-row');
      if (row) {
        activeCustomIdRow = row;
        syncCustomIdRowState(row);
        updateCustomIdPreview();
      }
    });

    bulkCustomIdContainer.addEventListener('change', function (event) {
      const row = event.target.closest('.bulk-customid-row');
      if (row) {
        activeCustomIdRow = row;
        syncCustomIdRowState(row);
        updateCustomIdPreview();
      }
    });

    activeCustomIdRow = getAllCustomIdRows()[0] || null;
    getAllCustomIdRows().forEach(syncCustomIdRowState);
    updateCustomIdPreview();
  }

  if (existingCustomIdRows.length > 0) {
    existingCustomIdRows.forEach(syncCustomIdRowState);
    existingCustomIdRows.forEach(function (row) {
      row.addEventListener('click', function () {
        activeCustomIdRow = row;
        updateCustomIdPreview();
      });

      row.querySelectorAll('input, select').forEach(function (input) {
        input.addEventListener('input', function () {
          activeCustomIdRow = row;
          syncCustomIdRowState(row);
          updateCustomIdPreview();
        });

        input.addEventListener('change', function () {
          activeCustomIdRow = row;
          syncCustomIdRowState(row);
          updateCustomIdPreview();
        });
      });
    });
    if (!activeCustomIdRow) {
      activeCustomIdRow = existingCustomIdRows[0];
    }
    updateCustomIdPreview();
  }

  function setInventoryPreview(src) {
    if (!inventoryPreviewImage || !inventoryPreviewPlaceholder) return;

    if (!src) {
      inventoryPreviewImage.style.display = 'none';
      inventoryPreviewImage.removeAttribute('src');
      inventoryPreviewPlaceholder.style.display = 'block';
      return;
    }

    inventoryPreviewImage.src = src;
    inventoryPreviewImage.style.display = 'block';
    inventoryPreviewPlaceholder.style.display = 'none';
  }

  if (inventoryImageUrlInput) {
    inventoryImageUrlInput.addEventListener('input', function () {
      if (inventoryImageFileInput && inventoryImageFileInput.files && inventoryImageFileInput.files.length > 0) {
        return;
      }
      setInventoryPreview(inventoryImageUrlInput.value.trim());
    });
  }

	  if (inventoryImageFileInput) {
	    inventoryImageFileInput.addEventListener('change', function () {
      const file = inventoryImageFileInput.files && inventoryImageFileInput.files[0];
      if (!file) {
        setInventoryPreview(inventoryImageUrlInput ? inventoryImageUrlInput.value.trim() : '');
        return;
      }

      const objectUrl = URL.createObjectURL(file);
	      setInventoryPreview(objectUrl);
	    });
	  }

	  function initAccessManager() {
	    if (!accessManager) return;

	    const searchUrl = accessManager.getAttribute('data-search-url');
	    const searchInput = accessManager.querySelector('[data-access-search-input]');
	    const resultsContainer = accessManager.querySelector('[data-access-search-results]');
	    const selectedList = accessManager.querySelector('[data-access-selected-list]');
	    const emptyState = accessManager.querySelector('[data-access-empty-state]');
	    const existingCount = document.querySelector('[data-access-existing-count]');
	    const existingBody = document.querySelector('[data-access-existing-body]');
	    let debounceHandle = null;

	    if (!searchUrl || !searchInput || !resultsContainer || !selectedList) return;

	    function selectedUserIds() {
	      return Array.from(selectedList.querySelectorAll('input[name="AccessForm.UserIds"]')).map(function (input) {
	        return input.value;
	      });
	    }

	    function syncEmptyState() {
	      if (!emptyState) return;
	      emptyState.classList.toggle('d-none', selectedList.querySelectorAll('.selected-access-user').length > 0);
	    }

	    function syncExistingAccessSummary() {
	      if (!existingCount) return;
	      const count = document.querySelectorAll('[data-access-existing-row]').length;
	      existingCount.textContent = `${count} user(s)`;

	      if (!existingBody) return;
	      const emptyRow = existingBody.querySelector('[data-access-empty-row]');
	      if (count === 0 && !emptyRow) {
	        const row = document.createElement('tr');
	        row.setAttribute('data-access-empty-row', '');
	        row.innerHTML = '<td colspan="6" class="text-muted">No users have explicit access yet.</td>';
	        existingBody.appendChild(row);
	      } else if (count > 0 && emptyRow) {
	        emptyRow.remove();
	      }
	    }

	    function addSelectedUser(user) {
	      if (!user || !user.id || selectedUserIds().includes(String(user.id))) return;

	      const item = document.createElement('div');
	      item.className = 'selected-access-user';
	      item.setAttribute('data-user-id', user.id);
	      item.innerHTML = `
	        <div class="selected-access-user-main">
	          <div class="fw-semibold">${user.userName}</div>
	          <div class="small text-muted">${user.email}</div>
	        </div>
	        <button class="btn btn-outline-danger btn-sm" type="button" data-remove-access-user>
	          <i class="fa-solid fa-xmark"></i>
	        </button>
	        <input type="hidden" name="AccessForm.UserIds" value="${user.id}" />`;
	      selectedList.appendChild(item);
	      syncEmptyState();
	    }

	    function renderResults(users) {
	      const selectedIds = selectedUserIds();
	      resultsContainer.innerHTML = '';

	      if (!users || users.length === 0) {
	        resultsContainer.classList.add('d-none');
	        return;
	      }

	      users.forEach(function (user) {
	        const isPersisted = !!user.isAlreadyAdded;
	        const isSelectedOnly = selectedIds.includes(String(user.id)) && !isPersisted;
	        const alreadySelected = isPersisted || isSelectedOnly;
	        const resultItem = document.createElement('button');
	        resultItem.type = 'button';
	        resultItem.className = 'list-group-item list-group-item-action access-search-result';
	        resultItem.disabled = alreadySelected;
	        resultItem.innerHTML = `
	          <div class="d-flex justify-content-between align-items-start gap-3">
	            <div>
	              <div class="fw-semibold">${user.userName}</div>
	              <div class="small text-muted">${user.email}</div>
	            </div>
	            <div class="text-end">
	              ${user.isAdmin ? '<span class="badge text-bg-warning">Admin</span>' : '<span class="badge text-bg-light">User</span>'}
	              ${isPersisted ? '<span class="badge text-bg-secondary ms-1">Added</span>' : ''}
	              ${isSelectedOnly ? '<span class="badge text-bg-info ms-1">Selected</span>' : ''}
	            </div>
	          </div>`;
	        resultItem.addEventListener('click', function () {
	          addSelectedUser(user);
	          searchInput.value = '';
	          resultsContainer.innerHTML = '';
	          resultsContainer.classList.add('d-none');
	        });
	        resultsContainer.appendChild(resultItem);
	      });

	      resultsContainer.classList.remove('d-none');
	    }

	    async function runSearch() {
	      const query = searchInput.value.trim();
	      if (query.length < 2) {
	        resultsContainer.innerHTML = '';
	        resultsContainer.classList.add('d-none');
	        return;
	      }

	      try {
	        const separator = searchUrl.includes('?') ? '&' : '?';
	        const response = await fetch(`${searchUrl}${separator}q=${encodeURIComponent(query)}`, {
	          headers: { 'X-Requested-With': 'XMLHttpRequest' }
	        });
	        if (!response.ok) {
	          resultsContainer.classList.add('d-none');
	          return;
	        }

	        const users = await response.json();
	        renderResults(users);
	      } catch (_error) {
	        resultsContainer.classList.add('d-none');
	      }
	    }

	    searchInput.addEventListener('input', function () {
	      clearTimeout(debounceHandle);
	      debounceHandle = window.setTimeout(runSearch, 250);
	    });

	    function removeSelectedUser(userId) {
	      if (!userId) return;
	      const selectedItem = selectedList.querySelector(`.selected-access-user[data-user-id="${userId}"]`);
	      if (selectedItem) {
	        selectedItem.remove();
	      }

	      const existingRow = document.querySelector(`[data-access-existing-row][data-user-id="${userId}"]`);
	      if (existingRow) {
	        existingRow.remove();
	      }

	      syncEmptyState();
	      syncExistingAccessSummary();
	    }

	    selectedList.addEventListener('click', function (event) {
	      const removeButton = event.target.closest('[data-remove-access-user]');
	      if (!removeButton) return;
	      const item = removeButton.closest('.selected-access-user');
	      if (!item) return;
	      removeSelectedUser(item.getAttribute('data-user-id'));
	    });

	    document.addEventListener('click', function (event) {
	      const tableRemoveButton = event.target.closest('[data-access-existing-row] [data-remove-access-user]');
	      if (tableRemoveButton) {
	        removeSelectedUser(tableRemoveButton.getAttribute('data-user-id'));
	        return;
	      }

	      if (!accessManager.contains(event.target)) {
	        resultsContainer.classList.add('d-none');
	      }
	    });

	    syncEmptyState();
	    syncExistingAccessSummary();
	  }

	  initAccessManager();

	  const discussionRoot = document.getElementById('inventoryDiscussionRoot');
  if (discussionRoot && window.signalR) {
    const inventoryId = discussionRoot.getAttribute('data-inventory-id');
    const hubUrl = discussionRoot.getAttribute('data-comment-hub-url');
    const commentList = document.getElementById('inventoryCommentList');
    const commentCount = document.getElementById('commentThreadCount');
    const emptyState = document.getElementById('inventoryCommentEmptyState');

    function formatLocalDate(value) {
      const date = new Date(value);
      if (Number.isNaN(date.getTime())) return 'Just now';
      return date.toLocaleString();
    }

    function updateCommentCount() {
      if (!commentList || !commentCount) return;
      commentCount.textContent = String(commentList.querySelectorAll('[data-comment-id]').length);
    }

    function appendRealtimeComment(payload) {
      if (!commentList || !payload || !payload.commentId) return;
      if (commentList.querySelector('[data-comment-id="' + payload.commentId + '"]')) return;

      if (emptyState) {
        emptyState.remove();
      }

      const wrapper = document.createElement('div');
      wrapper.className = 'card border';
      wrapper.setAttribute('data-comment-id', payload.commentId);
      wrapper.innerHTML = `
        <div class="card-body">
          <div class="d-flex justify-content-between align-items-start gap-3">
            <div>
              <div class="fw-semibold">${payload.userName || payload.userId || 'User'}</div>
              <div class="small text-muted">Posted ${formatLocalDate(payload.createdAtUtc)}</div>
            </div>
            <span class="badge text-bg-info">Live</span>
          </div>
          <div class="mt-3"></div>
        </div>`;

      const contentNode = wrapper.querySelector('.mt-3');
      if (contentNode) {
        contentNode.textContent = payload.content || '';
      }

      commentList.prepend(wrapper);
      updateCommentCount();
    }

    if (inventoryId && hubUrl) {
      const connection = new window.signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveNewComment', function (payload) {
        appendRealtimeComment(payload);
      });

      connection.start()
        .then(function () {
          return connection.invoke('JoinInventoryGroup', inventoryId);
        })
        .catch(function (error) {
          console.warn('Comment hub connection failed.', error);
        });

      window.addEventListener('beforeunload', function () {
        connection.invoke('LeaveInventoryGroup', inventoryId).catch(function () { });
      });
    }
  }
});
