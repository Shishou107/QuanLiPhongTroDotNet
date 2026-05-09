// js/contract.js
async function loadContracts() {
    const container = document.getElementById('room-container');
    const title = document.querySelector('h2.mt-4');
    
    title.innerText = "Danh Sách Hợp Đồng Thuê Phòng";
    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div> Đang tải dữ liệu...</div>`;

    try {
        const res = await fetch("https://localhost:7209/api/Contracts");
        const contracts = await res.json();
        
        container.innerHTML = ""; 

       // Thay thế đoạn let html = ... bằng đoạn này:
        let html = `
            <div class="card shadow border-0">
                <div class="card-header bg-primary text-white d-flex justify-content-between align-items-center">
                    <h5 class="mb-0"><i class="bi bi-file-earmark-text-fill me-2"></i>Danh sách Hợp đồng hiệu lực</h5>
                    <button class="btn btn-light btn-sm fw-bold" onclick="addNewContract()">
                        <i class="bi bi-plus-lg"></i> Lập hợp đồng mới
                    </button>
                </div>
                <div class="card-body p-0">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light">
                                <tr class="text-uppercase small fw-bold">
                                    <th class="ps-3">Mã Hợp Đồng</th>
                                    <th>Khách Thuê</th>
                                    <th>Phòng</th>
                                    <th>Thời Hạn</th>
                                    <th>Tiền Cọc</th>
                                    <th class="text-center">Trạng Thái</th>
                                    <th class="text-center">Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>`;

        contracts.forEach(c => {
            // Định dạng ngày và tiền
            const start = new Date(c.startDate).toLocaleDateString('vi-VN');
            const end = new Date(c.endDate).toLocaleDateString('vi-VN');
            const deposit = c.depositAmount.toLocaleString('vi-VN');
            
            // Trạng thái badge
            const statusBadge = c.status === 1 ? 'bg-success' : 'bg-secondary';
            const statusText = c.status === 1 ? 'Đang hiệu lực' : 'Đã kết thúc';

            // Xử lý chuỗi mã hợp đồng để tránh lỗi nếu null/undefined
            const contractIdDisplay = c.contractNumber ? c.contractNumber.substring(0, 8) : c.id.substring(0, 8);

            html += `
                <tr>
                    <td class="ps-3 fw-bold text-muted small">
                        #${contractIdDisplay}...
                    </td>
                    <td>
                        <div class="fw-bold text-primary">${c.tenantName}</div>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark fs-6">
                            <i class="bi bi-door-open-fill"></i> ${c.roomNumber}
                        </span>
                    </td>
                    <td class="small">
                        <span class="text-muted">Từ:</span> ${start} <br>
                        <span class="text-muted">Đến:</span> ${end}
                    </td>
                    <td class="fw-bold text-danger">${deposit} đ</td>
                    <td class="text-center">
                        <span class="badge ${statusBadge}">${statusText}</span>
                    </td>
                    <td class="text-center">
                       
<div class="btn-group">
    <button class="btn btn-outline-info btn-sm" onclick="viewContractSummary('${c.id}')" title="Xem Hóa Đơn / Công nợ">
        <i class="bi bi-receipt"></i>
    </button>
    <button class="btn btn-outline-warning btn-sm" onclick="editContract('${c.id}')" title="Sửa">
        <i class="bi bi-pencil"></i>
    </button>
    <button class="btn btn-outline-danger btn-sm" onclick="deleteContract('${c.id}', '${c.tenantName}', '${c.roomNumber}')" title="Xóa">
        <i class="bi bi-trash"></i>
    </button>
</div>
                    </td>
                </tr>`;
        });

        html += `</tbody></table></div></div></div>`;

        html += `</tbody></table></div></div>`;
        container.innerHTML = html;

    } catch (error) {
        container.innerHTML = `<div class="alert alert-danger">Lỗi nạp dữ liệu hợp đồng.</div>`;
    }
}
// js/contract.js

// ==========================================
// 1. HÀM HIỂN THỊ FORM THÊM HỢP ĐỒNG
// ==========================================
async function addNewContract() {
    const container = document.getElementById('room-container');
    document.querySelector('h2.mt-4').innerText = "Lập Hợp Đồng Mới";

    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div> Đang chuẩn bị dữ liệu...</div>`;

    try {
        // Lấy danh sách phòng và khách thuê để chọn
        const [resRooms, resTenants] = await Promise.all([
            fetch("https://localhost:7209/api/Rooms"),
            fetch("https://localhost:7209/api/Tenants")
        ]);

        const rooms = await resRooms.json();
        const tenants = await resTenants.json();

        // Chỉ lấy những phòng đang trống (Status = 0)
        const availableRooms = rooms.filter(r => r.status === 0);

        container.innerHTML = `
            <div class="card shadow border-0" style="max-width: 850px; margin: auto;">
                <div class="card-header bg-dark text-white">
                    <h5 class="mb-0"><i class="bi bi-file-earmark-plus me-2"></i>Tạo hợp đồng thuê mới</h5>
                </div>
                <div class="card-body">
                    <form id="form-new-contract">
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">CHỌN KHÁCH THUÊ</label>
                                <select id="c-tenantId" class="form-select" required>
                                    <option value="">-- Chọn khách hàng --</option>
                                    ${tenants.map(t => `<option value="${t.id}">${t.fullName} (${t.phoneNumber})</option>`).join('')}
                                </select>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">CHỌN PHÒNG TRỐNG</label>
                                <!-- Lưu ý: data-price dùng để tự động điền giá -->
                                <select id="c-roomId" class="form-select" required>
                                    <option value="" data-price="">-- Chọn phòng --</option>
                                    ${availableRooms.map(r => `
                                        <option value="${r.id}" data-price="${r.basePrice}">
                                            Phòng ${r.roomNumber} - ${r.basePrice.toLocaleString('vi-VN')}đ
                                        </option>
                                    `).join('')}
                                </select>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">NGÀY BẮT ĐẦU</label>
                                <input type="date" id="c-startDate" class="form-control" value="${new Date().toISOString().split('T')[0]}" required>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">NGÀY KẾT THÚC (Dự kiến)</label>
                                <input type="date" id="c-endDate" class="form-control" required>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">TIỀN ĐẶT CỌC (VNĐ)</label>
                                <input type="number" id="c-deposit" class="form-control" placeholder="0" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">GIÁ THUÊ/THÁNG (VNĐ)</label>
                                <input type="number" id="c-agreedPrice" class="form-control" placeholder="0" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">GHI CHÚ HỢP ĐỒNG</label>
                                <input type="text" id="c-note" class="form-control" placeholder="Nhập ghi chú...">
                            </div>
                        </div>

                        <div class="d-flex gap-2 mt-3">
                            <button type="submit" class="btn btn-dark w-100 fw-bold text-uppercase">Xác nhận ký hợp đồng</button>
                            <button type="button" class="btn btn-light w-100 fw-bold" onclick="loadContracts()">Quay lại</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        // Lắng nghe sự kiện Submit Form
        document.getElementById('form-new-contract').addEventListener('submit', saveNewContract);

        // Lắng nghe sự kiện đổi Phòng -> Tự động điền giá tiền
        document.getElementById('c-roomId').addEventListener('change', function() {
            const selectedOption = this.options[this.selectedIndex];
            const roomPrice = selectedOption.getAttribute('data-price');
            const agreedPriceInput = document.getElementById('c-agreedPrice');
            
            if (roomPrice) {
                agreedPriceInput.value = roomPrice;
            } else {
                agreedPriceInput.value = ""; 
            }
        });

    } catch (error) {
        alert("Lỗi khi tải dữ liệu khởi tạo hợp đồng!");
    }
}

// ==========================================
// 2. HÀM GỬI DỮ LIỆU TẠO HỢP ĐỒNG LÊN API
// ==========================================
async function saveNewContract(e) {
    e.preventDefault();

    const tenantId = document.getElementById('c-tenantId').value;
    const roomId = document.getElementById('c-roomId').value;
    const startDate = document.getElementById('c-startDate').value;
    const endDate = document.getElementById('c-endDate').value;

    // Kiểm tra chặn lỗi từ Frontend
    if (!tenantId || !roomId) {
        alert("Vui lòng chọn đầy đủ Khách thuê và Phòng trước khi tạo hợp đồng!");
        return; 
    }

    if (!startDate || !endDate) {
        alert("Vui lòng chọn đầy đủ ngày bắt đầu và kết thúc!");
        return;
    }

    // Đóng gói dữ liệu chuẩn theo Cấu trúc API C# của bạn
    const contractData = {
        id: "00000000-0000-0000-0000-000000000000", // Fix lỗi C# bắt buộc có Guid nhưng không cho trống
        roomId: roomId,
        tenantId: tenantId,
        startDate: startDate,
        endDate: endDate,
        depositAmount: parseFloat(document.getElementById('c-deposit').value) || 0,
        agreedPrice: parseFloat(document.getElementById('c-agreedPrice').value) || 0,
        status: 1, // 1 là Đang hiệu lực
        note: document.getElementById('c-note').value || "Hợp đồng tạo mới",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
    };

    try {
        const res = await fetch("https://localhost:7209/api/Contracts", {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(contractData)
        });

        if (res.ok) {
            alert("Lập hợp đồng thành công!");
            loadContracts(); // Tải lại danh sách
        } else {
            // Lấy chính xác lỗi từ C# trả về để báo cho người dùng
            const err = await res.json();
            console.error("Chi tiết lỗi từ Backend:", err);
            
            let errorMsg = "Không thể tạo hợp đồng. ";
            if (err.errors) {
                // Lấy thông báo lỗi cụ thể (ví dụ: thiếu trường dữ liệu, sai định dạng)
                errorMsg += Object.values(err.errors)[0][0]; 
            } else if (err.title) {
                errorMsg += err.title;
            }
            alert(errorMsg);
        }
    } catch (error) {
        console.error(error);
        alert("Lỗi kết nối đến máy chủ API.");
    }
}
// ==========================================
// 4. HÀM CẬP NHẬT HỢP ĐỒNG LÊN API
// ==========================================
async function updateContract(e) {
    e.preventDefault();
    const id = document.getElementById('edit-c-id').value;

    const updatedData = {
        id: id,
        roomId: document.getElementById('edit-c-roomId').value,
        tenantId: document.getElementById('edit-c-tenantId').value,
        startDate: document.getElementById('edit-c-startDate').value,
        endDate: document.getElementById('edit-c-endDate').value,
        depositAmount: parseFloat(document.getElementById('edit-c-deposit').value) || 0,
        agreedPrice: parseFloat(document.getElementById('edit-c-agreedPrice').value) || 0,
        status: parseInt(document.getElementById('edit-c-status').value),
        note: document.getElementById('edit-c-note').value,
        updatedAt: new Date().toISOString() // Cập nhật lại thời gian sửa
    };

    try {
        const res = await fetch(`https://localhost:7209/api/Contracts/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updatedData)
        });

        if (res.ok) {
            alert("Đã cập nhật hợp đồng thành công!");
            loadContracts();
        } else {
            const err = await res.json();
            alert("Lỗi cập nhật: " + (err.title || "Vui lòng kiểm tra lại dữ liệu"));
        }
    } catch (error) {
        console.error("Lỗi:", error);
        alert("Lỗi kết nối máy chủ API.");
    }
}
// ==========================================
// 5. HÀM XÓA HỢP ĐỒNG
// ==========================================
async function deleteContract(id, tenantName, roomNumber) {
    const confirmDelete = confirm(`CẢNH BÁO: Bạn có chắc chắn muốn xóa hợp đồng của:\nKhách thuê: ${tenantName}\nPhòng: ${roomNumber}?\n\nHành động này không thể hoàn tác!`);

    if (confirmDelete) {
        try {
            const res = await fetch(`https://localhost:7209/api/Contracts/${id}`, {
                method: 'DELETE'
            });

            if (res.ok) {
                alert("Đã xóa hợp đồng thành công!");
                loadContracts();
            } else {
                alert("Không thể xóa hợp đồng này. Vui lòng kiểm tra ràng buộc dữ liệu phía máy chủ.");
            }
        } catch (error) {
            console.error("Lỗi xóa:", error);
            alert("Lỗi kết nối đến máy chủ.");
        }
    }
}
// ==========================================
// 3. HÀM HIỂN THỊ FORM SỬA HỢP ĐỒNG
// ==========================================
async function editContract(id) {
    const container = document.getElementById('room-container');
    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-warning"></div> Đang lấy thông tin hợp đồng...</div>`;

    try {
        // Lấy dữ liệu hợp đồng hiện tại, danh sách phòng và khách
        const [resContract, resRooms, resTenants] = await Promise.all([
            fetch(`https://localhost:7209/api/Contracts/${id}`),
            fetch("https://localhost:7209/api/Rooms"),
            fetch("https://localhost:7209/api/Tenants")
        ]);

        const c = await resContract.json();
        const rooms = await resRooms.json();
        const tenants = await resTenants.json();

        // Lọc phòng trống HOẶC phòng đang được gán cho chính hợp đồng này
        const availableRooms = rooms.filter(r => r.status === 0 || r.id === c.roomId);

        // Xử lý định dạng ngày tháng để hiển thị đúng trên thẻ <input type="date">
        const start = c.startDate ? c.startDate.split('T')[0] : '';
        const end = c.endDate ? c.endDate.split('T')[0] : '';

        document.querySelector('h2.mt-4').innerText = "Chỉnh Sửa Hợp Đồng";
        container.innerHTML = `
            <div class="card shadow border-0" style="max-width: 850px; margin: auto;">
                <div class="card-header bg-warning text-dark">
                    <h5 class="mb-0"><i class="bi bi-pencil-square me-2"></i>Cập nhật thông tin hợp đồng</h5>
                </div>
                <div class="card-body">
                    <form id="form-edit-contract">
                        <input type="hidden" id="edit-c-id" value="${c.id}">
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">KHÁCH THUÊ</label>
                                <select id="edit-c-tenantId" class="form-select" required>
                                    ${tenants.map(t => `<option value="${t.id}" ${t.id === c.tenantId ? 'selected' : ''}>${t.fullName}</option>`).join('')}
                                </select>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">PHÒNG</label>
                                <select id="edit-c-roomId" class="form-select" required>
                                    ${availableRooms.map(r => `
                                        <option value="${r.id}" data-price="${r.basePrice}" ${r.id === c.roomId ? 'selected' : ''}>
                                            Phòng ${r.roomNumber} - ${r.basePrice.toLocaleString('vi-VN')}đ
                                        </option>
                                    `).join('')}
                                </select>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">NGÀY BẮT ĐẦU</label>
                                <input type="date" id="edit-c-startDate" class="form-control" value="${start}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">NGÀY KẾT THÚC</label>
                                <input type="date" id="edit-c-endDate" class="form-control" value="${end}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">TRẠNG THÁI</label>
                                <select id="edit-c-status" class="form-select">
                                    <option value="1" ${c.status === 1 ? 'selected' : ''}>Đang hiệu lực</option>
                                    <option value="0" ${c.status === 0 ? 'selected' : ''}>Đã kết thúc</option>
                                </select>
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">TIỀN ĐẶT CỌC</label>
                                <input type="number" id="edit-c-deposit" class="form-control" value="${c.depositAmount}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">GIÁ THUÊ/THÁNG</label>
                                <input type="number" id="edit-c-agreedPrice" class="form-control" value="${c.agreedPrice}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label fw-bold small">GHI CHÚ</label>
                                <input type="text" id="edit-c-note" class="form-control" value="${c.note || ''}">
                            </div>
                        </div>

                        <div class="d-flex gap-2 mt-3">
                            <button type="submit" class="btn btn-warning w-100 fw-bold">LƯU THAY ĐỔI</button>
                            <button type="button" class="btn btn-light w-100 fw-bold" onclick="loadContracts()">HỦY BỎ</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        document.getElementById('form-edit-contract').addEventListener('submit', updateContract);

        // Tự động điền lại giá khi đổi sang phòng khác (Giống form thêm mới)
        document.getElementById('edit-c-roomId').addEventListener('change', function() {
            const selectedOption = this.options[this.selectedIndex];
            const roomPrice = selectedOption.getAttribute('data-price');
            const agreedPriceInput = document.getElementById('edit-c-agreedPrice');
            if (roomPrice) agreedPriceInput.value = roomPrice;
        });

    } catch (error) {
        alert("Không thể tải thông tin hợp đồng này!");
        loadContracts();
    }
}
// ==========================================
// 6. HIỂN THỊ CHI TIẾT CÔNG NỢ & HÓA ĐƠN
// ==========================================
async function viewContractSummary(contractId) {
    console.log("Đang gọi Summary cho ID:", contractId); // Debug xem ID có bị undefined không

    if (!contractId || contractId === 'undefined') {
        console.error("Contract ID không hợp lệ!");
        alert("Không thể xác định mã hợp đồng. Đang quay lại danh sách...");
        loadContracts(); // Quay về danh sách chính nếu lỗi ID
        return;
    }

    const container = document.getElementById('room-container');
    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div> Đang tải dữ liệu...</div>`;

    try {
        const res = await fetch(`https://localhost:7209/api/Invoice/Contract/${contractId}/Summary`);
        
        if (!res.ok) {
            const errorText = await res.text();
            throw new Error(errorText || "Lỗi server");
        }

        const data = await res.json();

        // Vẽ giao diện Thống kê (Cards)
        let html = `
            <div class="mb-3 d-flex justify-content-between align-items-center">
                <h5 class="text-primary fw-bold mb-0">
                    <i class="bi bi-person-badge"></i> Khách thuê: ${data.tenantName} - Phòng: ${data.roomNumber}
                </h5>
                <button class="btn btn-secondary btn-sm fw-bold" onclick="loadContracts()">
                    <i class="bi bi-arrow-left"></i> Quay lại danh sách
                </button>
            </div>

            <div class="row mb-4">
                <div class="col-md-3">
                    <div class="card bg-light border-0 shadow-sm">
                        <div class="card-body text-center">
                            <h6 class="text-muted small fw-bold">GIÁ THUÊ/THÁNG</h6>
                            <h4 class="text-dark fw-bold mb-0">${(data.agreedPrice || 0).toLocaleString('vi-VN')} đ</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-info text-white border-0 shadow-sm">
                        <div class="card-body text-center">
                            <h6 class="small fw-bold">SỐ THÁNG ĐÃ Ở</h6>
                            <h4 class="fw-bold mb-0">${data.totalMonthsElapsed} tháng</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-success text-white border-0 shadow-sm">
                        <div class="card-body text-center">
                            <h6 class="small fw-bold">ĐÃ ĐÓNG</h6>
                            <h4 class="fw-bold mb-0">${data.paidMonths} tháng</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card ${data.totalDebt > 0 ? 'bg-danger' : 'bg-secondary'} text-white border-0 shadow-sm">
                        <div class="card-body text-center">
                            <h6 class="small fw-bold">TỔNG TIỀN ĐANG NỢ</h6>
                            <h4 class="fw-bold mb-0">${data.totalDebt.toLocaleString('vi-VN')} đ</h4>
                        </div>
                    </div>
                </div>
            </div>

            <div class="card shadow border-0">
                <div class="card-header bg-dark text-white">
                    <h5 class="mb-0"><i class="bi bi-calendar-check me-2"></i>Chi tiết các kỳ thanh toán</h5>
                </div>
                <div class="card-body p-0">
                    <div class="table-responsive">
                        <table class="table table-hover align-middle mb-0">
                            <thead class="table-light">
                                <tr class="small fw-bold text-uppercase">
                                    <th class="ps-3">Kỳ (Tháng)</th>
                                    <th>Thời gian thu</th>
                                    <th>Cần thanh toán</th>
                                    <th class="text-center">Trạng thái</th>
                                    <th class="text-center">Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>
        `;

        // Lặp qua danh sách các tháng (cycles)
        if (data.cycles.length === 0) {
            html += `<tr><td colspan="5" class="text-center py-4 text-muted">Chưa phát sinh chu kỳ thanh toán nào.</td></tr>`;
        } else {
            data.cycles.forEach(cycle => {
                const start = new Date(cycle.cycleStart);
                const end = new Date(cycle.cycleEnd);
                const startStr = start.toLocaleDateString('vi-VN');
                const endStr = end.toLocaleDateString('vi-VN');
                
                // Lấy tháng và năm của chu kỳ để hiển thị cho đẹp (VD: Tháng 04/2026)
                const monthYear = `Tháng ${start.getMonth() + 1}/${start.getFullYear()}`;

                const isPaid = cycle.isPaid;
                const statusBadge = isPaid ? 'bg-success' : 'bg-danger';
                const statusText = isPaid ? 'Đã Thanh Toán' : 'Chưa Thanh Toán';

                html += `
                    <tr>
                        <td class="ps-3 fw-bold text-primary">Kỳ ${cycle.cycleNumber}: ${monthYear}</td>
                        <td class="small">Từ ${startStr} - Đến ${endStr}</td>
                        <td class="fw-bold ${isPaid ? 'text-success' : 'text-danger'}">
                            ${cycle.expectedAmount.toLocaleString('vi-VN')} đ
                        </td>
                        <td class="text-center">
                            <span class="badge ${statusBadge} px-2 py-1">${statusText}</span>
                        </td>
                        <td class="text-center">
                            ${isPaid ? 
                                `<button class="btn btn-sm btn-outline-success" onclick="viewInvoiceDetail('${cycle.invoiceId}')">
                                    <i class="bi bi-eye"></i> Xem hóa đơn
                                 </button>` 
                                : 
                                `<button class="btn btn-sm btn-primary fw-bold" onclick="createInvoice('${data.contractId}', ${start.getMonth() + 1}, ${start.getFullYear()}, ${cycle.expectedAmount})">
                                    <i class="bi bi-plus-circle"></i> Lập hóa đơn
                                 </button>`
                            }
                        </td>
                    </tr>
                `;
            });
        }

        html += `</tbody></table></div></div></div>`;
        container.innerHTML = html;

    } catch (error) {
        console.error(error);
        alert("Lỗi kết nối máy chủ API.");
    }
}

// Hàm giả (Placeholder) để sau này bạn làm chức năng lập hóa đơn
// ==========================================
// 7. HÀM XỬ LÝ LẬP HÓA ĐƠN / THU TIỀN
// ==========================================
function createInvoice(contractId, month, year, baseAmount) {
    const container = document.getElementById('room-container');
    document.querySelector('h2.mt-4').innerText = `Lập Hóa Đơn - Tháng ${month}/${year}`;

    // Vẽ Form nhập thông tin hóa đơn
    container.innerHTML = `
        <div class="card shadow border-0" style="max-width: 600px; margin: auto;">
            <div class="card-header bg-primary text-white">
                <h5 class="mb-0"><i class="bi bi-receipt me-2"></i>Thông tin thu tiền</h5>
            </div>
            <div class="card-body">
                <form id="form-create-invoice">
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">KỲ THANH TOÁN</label>
                            <input type="text" class="form-control" value="Tháng ${month}/${year}" disabled>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">TRẠNG THÁI</label>
                            <select id="inv-status" class="form-select">
                                <option value="1">Đã thu tiền (Tiền mặt/CK)</option>
                                <option value="0">Chưa thu (Ghi nợ)</option>
                            </select>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label fw-bold small">TIỀN PHÒNG (GỐC)</label>
                        <input type="number" class="form-control text-muted" value="${baseAmount}" disabled>
                    </div>

                    <div class="mb-3">
                        <label class="form-label fw-bold small text-primary">TỔNG TIỀN THỰC THU (VNĐ)</label>
                        <input type="number" id="inv-total" class="form-control fw-bold border-primary" value="${baseAmount}" required>
                        <small class="text-muted fst-italic">Bạn có thể sửa số này (cộng thêm tiền điện, nước, rác...)</small>
                    </div>

                    <div class="d-flex gap-2 mt-4">
                        <button type="submit" class="btn btn-primary w-100 fw-bold">
                            <i class="bi bi-check-circle"></i> XÁC NHẬN LẬP HÓA ĐƠN
                        </button>
                        <button type="button" class="btn btn-light w-100 fw-bold" onclick="viewContractSummary('${contractId}')">
                            HỦY BỎ
                        </button>
                    </div>
                </form>
            </div>
        </div>
    `;

    // Bắt sự kiện khi bấm nút XÁC NHẬN
    document.getElementById('form-create-invoice').addEventListener('submit', async function(e) {
        e.preventDefault();

        const statusValue = parseInt(document.getElementById('inv-status').value);
        const totalAmountValue = parseFloat(document.getElementById('inv-total').value);

        // Đóng gói dữ liệu gửi lên API
    const invoiceData = {
    // Chỉ gửi các trường thuộc tính cơ bản
    id: "00000000-0000-0000-0000-000000000000",
    contractId: contractId, 
    billingMonth: parseInt(month),
    billingYear: parseInt(year),
    totalAmount: parseFloat(document.getElementById('inv-total').value),
    paidAmount: document.getElementById('inv-status').value === "1" 
                ? parseFloat(document.getElementById('inv-total').value) 
                : 0,
    status: parseInt(document.getElementById('inv-status').value),
    dueDate: new Date().toISOString().split('T')[0] // Trả về yyyy-mm-dd cho DateOnly
};

// KIỂM TRA: Không được có trường 'contract', 'invoiceDetails' hay 'payments' ở đây
console.log("Data gửi đi:", invoiceData);

const res = await fetch("https://localhost:7209/api/Invoice", {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(invoiceData)
});

        try {
            const res = await fetch("https://localhost:7209/api/Invoice", {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(invoiceData)
            });
            if (res.ok) {
                alert(`Đã lập hóa đơn cho Tháng ${month}/${year} thành công!`);
                // Quay lại ngay màn hình chi tiết công nợ để xem sự thay đổi
                viewContractSummary(contractId); 
            } else {
                const err = await res.json();
                alert(err.message || "Có lỗi xảy ra khi lập hóa đơn!");
            }
        } catch (error) {
            console.error(error);
            alert("Lỗi kết nối máy chủ API.");
        }
    });
}

// ==========================================
// 8. XEM CHI TIẾT BIÊN LAI (INVOICE)
// ==========================================
async function viewInvoiceDetail(invoiceId) {
    if (!invoiceId || invoiceId === 'null') return;
    const container = document.getElementById('room-container');
    
    try {
        const res = await fetch(`https://localhost:7209/api/Invoice/${invoiceId}`);
        const inv = await res.json();

        // Xác định màu sắc trạng thái
        const statusClass = inv.status === 1 ? 'alert-success' : 'alert-danger';
        const statusText = inv.status === 1 ? 'ĐÃ THANH TOÁN' : 'CHƯA THANH TOÁN';

        container.innerHTML = `
            <div class="card shadow-lg border-0 mx-auto" style="max-width: 550px; font-family: 'Courier New', Courier, monospace;">
                <div class="card-body p-4">
                    <div class="text-center">
                        <h4 class="fw-bold mb-0">BIÊN LAI CHI TIẾT</h4>
                        <p class="small text-muted">Mã: ${inv.id.substring(0, 8).toUpperCase()}</p>
                    </div>
                    
                    <div class="row small mt-3">
                        <div class="col-6"><b>Kỳ:</b> Tháng ${inv.billingMonth}/${inv.billingYear}</div>
                        <div class="col-6 text-end"><b>Ngày lập:</b> ${new Date(inv.createdAt).toLocaleDateString('vi-VN')}</div>
                    </div>
                    
                    <hr border-style="dashed">
                    
                    <table class="table table-sm table-borderless small">
                        <thead>
                            <tr class="border-bottom">
                                <th>Nội dung</th>
                                <th class="text-end">SL</th>
                                <th class="text-end">Đ.Giá</th>
                                <th class="text-end">T.Tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${inv.details.length > 0 ? inv.details.map(d => `
                                <tr>
                                    <td>${d.description}</td>
                                    <td class="text-end">${Number(d.quantity)}</td>
                                    <td class="text-end">${Number(d.unitPrice).toLocaleString()}</td>
                                    <td class="text-end fw-bold">${Number(d.amount).toLocaleString()}</td>
                                </tr>
                            `).join('') : '<tr><td colspan="4" class="text-center text-muted">Không có chi tiết dịch vụ</td></tr>'}
                        </tbody>
                    </table>
                    
                    <hr border-style="dashed">
                    
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <span class="fw-bold fs-6">TỔNG CỘNG:</span>
                        <span class="fw-bold text-danger fs-4">${inv.totalAmount.toLocaleString()} VNĐ</span>
                    </div>

                    <div class="alert ${statusClass} text-center fw-bold py-2 mb-3">
                        ${statusText}
                    </div>

                    <div class="d-flex gap-2 no-print">
                        <button class="btn btn-dark btn-sm w-100" onclick="window.print()">
                            <i class="bi bi-printer"></i> In biên lai
                        </button>
                        <button class="btn btn-outline-secondary btn-sm w-100" onclick="viewContractSummary('${inv.contractId}')">
                            Quay lại
                        </button>
                    </div>
                </div>
            </div>
        `;
    } catch (error) {
        console.error("Lỗi:", error);
        alert("Không thể tải chi tiết hóa đơn.");
    }
}