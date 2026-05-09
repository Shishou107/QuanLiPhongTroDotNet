const SERVICE_API = "https://localhost:7209/api/Services";

// Hàm khởi tạo khi bấm vào menu Dịch vụ
// js/services.js
async function loadServices() {
    console.log("Đang gọi hàm loadServices..."); // Kiểm tra xem hàm có chạy không
    const container = document.getElementById('room-container');
    
    if (!container) {
        console.error("Không tìm thấy thẻ có id='room-container'");
        return;
    }

    // Hiển thị tiêu đề và nút thêm
    container.innerHTML = `
        <div class="col-12 mb-4 d-flex justify-content-between align-items-center">
            <h3 class="fw-bold text-dark mb-0">Quản lý Dịch vụ</h3>
            <button class="btn btn-primary fw-bold" onclick="showAddServiceModal()">
                <i class="bi bi-plus-lg"></i> Thêm dịch vụ
            </button>
        </div>
        <div id="services-list" class="row">
            <div class="text-center py-5">Đang tải dữ liệu...</div>
        </div>
    `;

    try {
        const res = await fetch("https://localhost:7209/api/Services");
        if (!res.ok) throw new Error("Lỗi kết nối API");
        
        const services = await res.json();
        const listArea = document.getElementById('services-list');
        listArea.innerHTML = "";

        if (services.length === 0) {
            listArea.innerHTML = '<div class="alert alert-info">Chưa có dịch vụ nào được tạo.</div>';
            return;
        }

        services.forEach(s => {
            listArea.innerHTML += `
                <div class="col-md-4 mb-4">
                    <div class="card shadow-sm h-100">
                        <div class="card-body">
                            <h5 class="fw-bold">${s.name}</h5>
                            <p class="text-muted small mb-1">Đơn vị: ${s.unit}</p>
                            <p class="text-danger fw-bold fs-5">${s.unitPrice.toLocaleString()} đ</p>
                            <div class="d-flex gap-2">
                                <button class="btn btn-sm btn-outline-warning w-100" onclick="editService('${s.id}')">Sửa</button>
                                <button class="btn btn-sm btn-outline-danger w-100" onclick="deleteService('${s.id}', '${s.name}')">Xóa</button>
                            </div>
                        </div>
                    </div>
                </div>`;
        });
    } catch (error) {
        console.error("Lỗi fetch services:", error);
        document.getElementById('services-list').innerHTML = 
            `<div class="alert alert-danger">Không thể tải dịch vụ. Lỗi: ${error.message}</div>`;
    }
}

// 1. Hàm hiển thị Form thêm mới
function showAddServiceModal() {
    const content = document.getElementById('modal-content');
    const myModal = new bootstrap.Modal(document.getElementById('roomModal'));
    
    content.innerHTML = `
        <div class="p-3">
            <h5 class="fw-bold mb-3 text-primary"><i class="bi bi-plus-circle me-2"></i>Thêm dịch vụ mới</h5>
            <div class="mb-3">
                <label class="small fw-bold">TÊN DỊCH VỤ</label>
                <input type="text" id="s-name" class="form-control" placeholder="VD: Tiền điện, Phí rác...">
            </div>
            <div class="mb-3">
                <label class="small fw-bold">ĐƠN VỊ TÍNH</label>
                <input type="text" id="s-unit" class="form-control" placeholder="VD: kWh, Khối, Tháng...">
            </div>
            <div class="mb-3">
                <label class="small fw-bold">ĐƠN GIÁ (VNĐ)</label>
                <input type="number" id="s-price" class="form-control" placeholder="3500">
            </div>
            <button class="btn btn-success w-100 fw-bold py-2 mt-2" onclick="saveNewService()">
                <i class="bi bi-save me-1"></i> XÁC NHẬN LƯU
            </button>
        </div>
    `;
    myModal.show();
}

// 2. Logic gửi yêu cầu POST (Thêm mới)
async function saveNewService() {
    const data = {
        id: "00000000-0000-0000-0000-000000000000",
        name: document.getElementById('s-name').value,
        unit: document.getElementById('s-unit').value,
        unitPrice: parseFloat(document.getElementById('s-price').value) || 0,
        createdAt: new Date().toISOString()
    };

    if (!data.name || data.unitPrice < 0) {
        alert("Vui lòng nhập tên dịch vụ và đơn giá hợp lệ!");
        return;
    }

    try {
        const res = await fetch(SERVICE_API, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (res.ok) {
            alert("Đã thêm dịch vụ thành công!");
            bootstrap.Modal.getInstance(document.getElementById('roomModal')).hide();
            loadServices(); // Tải lại danh sách
        }
    } catch (error) {
        console.error("Lỗi:", error);
    }
}

// 3. Hàm hiển thị Form chỉnh sửa
async function editService(id) {
    const content = document.getElementById('modal-content');
    const myModal = new bootstrap.Modal(document.getElementById('roomModal'));

    try {
        const res = await fetch(`${SERVICE_API}/${id}`);
        const s = await res.json();

        content.innerHTML = `
            <div class="p-3">
                <h5 class="fw-bold mb-3 text-warning"><i class="bi bi-pencil-square me-2"></i>Chỉnh sửa dịch vụ</h5>
                <div class="mb-3">
                    <label class="small fw-bold">TÊN DỊCH VỤ</label>
                    <input type="text" id="edit-s-name" class="form-control" value="${s.name}">
                </div>
                <div class="mb-3">
                    <label class="small fw-bold">ĐƠN VỊ TÍNH</label>
                    <input type="text" id="edit-s-unit" class="form-control" value="${s.unit}">
                </div>
                <div class="mb-3">
                    <label class="small fw-bold">ĐƠN GIÁ (VNĐ)</label>
                    <input type="number" id="edit-s-price" class="form-control" value="${s.unitPrice}">
                </div>
                <button class="btn btn-primary w-100 fw-bold py-2 mt-2" onclick="updateService('${id}')">
                    <i class="bi bi-check-lg me-1"></i> CẬP NHẬT THAY ĐỔI
                </button>
            </div>
        `;
        myModal.show();
    } catch (error) {
        alert("Không thể lấy dữ liệu dịch vụ!");
    }
}

// 4. Logic gửi yêu cầu PUT (Cập nhật)
async function updateService(id) {
    const data = {
        id: id,
        name: document.getElementById('edit-s-name').value,
        unit: document.getElementById('edit-s-unit').value,
        unitPrice: parseFloat(document.getElementById('edit-s-price').value) || 0,
        updatedAt: new Date().toISOString()
    };

    try {
        const res = await fetch(`${SERVICE_API}/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (res.ok) {
            alert("Cập nhật thành công!");
            bootstrap.Modal.getInstance(document.getElementById('roomModal')).hide();
            loadServices();
        }
    } catch (error) {
        console.error("Lỗi cập nhật:", error);
    }
}

// 5. Logic gửi yêu cầu DELETE (Xóa)
async function deleteService(id, name) {
    if (confirm(`Xác nhận xóa dịch vụ: ${name}?\nLưu ý: Không thể xóa nếu dịch vụ này đã có trong hóa đơn.`)) {
        try {
            const res = await fetch(`${SERVICE_API}/${id}`, { method: 'DELETE' });
            
            if (res.ok) {
                alert("Đã xóa dịch vụ!");
                loadServices();
            } else {
                const err = await res.json();
                alert(err.message || "Xóa thất bại!");
            }
        } catch (error) {
            console.error("Lỗi xóa:", error);
        }
    }
}
// Thêm vào js/services.js
function addInvoiceDetailRow() {
    const detailContainer = document.getElementById('extra-services-list');
    const rowId = Date.now(); // Tạo ID duy nhất cho mỗi dòng
    
    const rowHtml = `
        <div class="row mb-2 border-bottom pb-2" id="row-${rowId}">
            <div class="col-6">
                <input type="text" class="form-control form-control-sm detail-desc" placeholder="Tên dịch vụ (vđ: Giặt đồ)">
            </div>
            <div class="col-4">
                <input type="number" class="form-control form-control-sm detail-amount" placeholder="Số tiền">
            </div>
            <div class="col-2">
                <button class="btn btn-sm btn-outline-danger" onclick="document.getElementById('row-${rowId}').remove()">X</button>
            </div>
        </div>
    `;
    detailContainer.insertAdjacentHTML('beforeend', rowHtml);
}
// ==========================================================
// LOGIC LẬP HÓA ĐƠN & GHI CHÚ DỊCH VỤ (TÍNH TIỀN CUỐI THÁNG)
// ==========================================================

// 1. Hàm hiển thị Modal Lập hóa đơn (Xác định ai, phòng nào, dùng gì)
async function showCreateInvoiceModal(contractId) {
    try {
        // Lấy thông tin hợp đồng để biết giá phòng và tên khách
        const res = await fetch(`https://localhost:7209/api/Contracts/${contractId}`);
        const contract = await res.json();

        const content = document.getElementById('modal-content');
        const myModal = new bootstrap.Modal(document.getElementById('roomModal'));

        content.innerHTML = `
            <div class="p-3">
                <h5 class="fw-bold text-primary mb-1">Lập Hóa Đơn Tháng ${new Date().getMonth() + 1}</h5>
                <p class="text-muted small mb-3">Khách: ${contract.tenant.fullName} - Phòng: ${contract.room.roomNumber}</p>
                <hr>

                <!-- 1. Chỉ số Điện/Nước -->
                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="small fw-bold text-secondary">SỐ ĐIỆN MỚI</label>
                        <input type="number" id="inv-dien-moi" class="form-control" placeholder="0">
                    </div>
                    <div class="col-6">
                        <label class="small fw-bold text-secondary">SỐ NƯỚC MỚI</label>
                        <input type="number" id="inv-nuoc-moi" class="form-control" placeholder="0">
                    </div>
                </div>

                <!-- 2. Khu vực Ghi chú các món mua thêm -->
                <div class="bg-light p-2 rounded border">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <label class="small fw-bold text-primary">DỊCH VỤ PHÁT SINH (GHI CHÚ)</label>
                        <button class="btn btn-sm btn-outline-primary py-0" onclick="addInvoiceDetailRow()">+ Thêm</button>
                    </div>
                    <div id="extra-services-list">
                        <!-- Dòng ghi chú mua thêm sẽ hiện ở đây -->
                    </div>
                </div>

                <!-- 3. Ghi chú nội bộ (Lời nhắn) -->
                <div class="mt-3">
                    <label class="small fw-bold text-secondary">GHI CHÚ TRÊN HÓA ĐƠN</label>
                    <textarea id="inv-note" class="form-control form-control-sm" rows="2" placeholder="Ví dụ: Đã trừ tiền cọc, khách thanh toán trễ..."></textarea>
                </div>

                <div class="mt-4">
                    <button class="btn btn-danger w-100 fw-bold py-2 shadow-sm" onclick="processSubmitInvoice('${contractId}', ${contract.agreedPrice})">
                        XÁC NHẬN & TÍNH TỔNG TIỀN
                    </button>
                </div>
            </div>
        `;
        myModal.show();
    } catch (error) {
        console.error("Lỗi lấy thông tin hợp đồng:", error);
    }
}

// 2. Logic gom dữ liệu và gửi lên Backend
async function processSubmitInvoice(contractId, giaPhong) {
    // Thu thập các dòng "mua thêm"
    const extraRows = document.querySelectorAll('#extra-services-list .row');
    let invoiceDetails = [];

    // Luôn luôn có dòng Tiền Phòng
    invoiceDetails.push({
        description: "Tiền thuê phòng tháng " + (new Date().getMonth() + 1),
        quantity: 1,
        unitPrice: giaPhong,
        amount: giaPhong
    });

    // Lấy chỉ số điện nước (giả định đơn giá 3.5k và 20k)
    const dienMoi = parseFloat(document.getElementById('inv-dien-moi').value) || 0;
    const nuocMoi = parseFloat(document.getElementById('inv-nuoc-moi').value) || 0;
    
    if (dienMoi > 0) {
        invoiceDetails.push({
            description: `Tiền điện (Chỉ số: ${dienMoi})`,
            quantity: 1,
            unitPrice: dienMoi * 3500, // Bạn có thể sửa logic lấy đơn giá từ API Service ở đây
            amount: dienMoi * 3500
        });
    }

    // Lấy các món mua thêm từ ghi chú
    extraRows.forEach(row => {
        const desc = row.querySelector('.detail-desc').value;
        const amt = parseFloat(row.querySelector('.detail-amount').value) || 0;
        if (desc && amt > 0) {
            invoiceDetails.push({
                description: desc,
                quantity: 1,
                unitPrice: amt,
                amount: amt
            });
        }
    });

    const payload = {
        contractId: contractId,
        invoiceDate: new Date().toISOString(),
        note: document.getElementById('inv-note').value,
        invoiceDetails: invoiceDetails // Backend sẽ tự Sum mảng này để ra TotalAmount
    };

    try {
        const res = await fetch("https://localhost:7209/api/Invoices", {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Lập hóa đơn thành công! Hệ thống đã tự cộng tổng tiền.");
            bootstrap.Modal.getInstance(document.getElementById('roomModal')).hide();
        }
    } catch (error) {
        console.error("Lỗi gửi hóa đơn:", error);
    }
}