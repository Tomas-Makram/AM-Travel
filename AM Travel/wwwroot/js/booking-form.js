function addPhoneRow() {
    const container = document.getElementById('phoneRows');
    const index = container.querySelectorAll('.phone-row').length;
    const row = document.createElement('div');
    row.className = 'phone-row';
    row.innerHTML = `
        <input name="PhoneNumbers[${index}].PhoneNumber" class="form-control" placeholder="01xxxxxxxxx" />
        <label class="switch-line compact">
            <input type="checkbox" name="PhoneNumbers[${index}].Prime" value="true" />
            <input type="hidden" name="PhoneNumbers[${index}].Prime" value="false" />
            <span>Primary</span>
        </label>
        <button type="button" class="btn btn-sm btn-danger" onclick="removePhoneRow(this)">Remove</button>
    `;
    container.appendChild(row);
}

function removePhoneRow(button) {
    const row = button.closest('.phone-row');
    if (row) row.remove();
    reindexPhoneRows();
}

function reindexPhoneRows() {
    document.querySelectorAll('#phoneRows .phone-row').forEach((row, index) => {
        const phone = row.querySelector('input[name$=".PhoneNumber"]');
        const prime = row.querySelector('input[type="checkbox"]');
        const hidden = row.querySelector('input[type="hidden"]');
        if (phone) phone.name = `PhoneNumbers[${index}].PhoneNumber`;
        if (prime) prime.name = `PhoneNumbers[${index}].Prime`;
        if (hidden) hidden.name = `PhoneNumbers[${index}].Prime`;
    });
}
