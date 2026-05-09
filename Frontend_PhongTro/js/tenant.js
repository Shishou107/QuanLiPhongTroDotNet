// js/tenant.js
async function loadTenants() {
    const container = document.getElementById('room-container');
    const title = document.querySelector('h2.mt-4');
    
    title.innerText = "Quản Lý Thông Tin Người Thuê";
    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary"></div> Đang kết nối dữ liệu khách thuê...</div>`;

    try {
        const res = await fetch("https://localhost:7209/api/Tenants");
        const tenants = await res.json();
        
        container.innerHTML = ""; // Xóa loading

        let html = `
            <div class="card shadow border-0">
                <div class="card-header bg-primary text-white d-flex justify-content-between align-items-center">
                    <h5 class="mb-0"><i class="bi bi-people-fill me-2"></i>Danh sách khách thuê hiện tại</h5>
                    
                    <button class="btn btn-light btn-sm fw-bold" onclick="addNewTenant()"><i class="bi bi-plus-lg"></i> Thêm khách mới</button>
                </div>
                <div class="card-body p-0">
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr class="text-uppercase small fw-bold">
                                    <th class="ps-3">Họ và Tên</th>
                                    <th>Thông tin liên lạc</th>
                                    <th>Định danh (CCCD)</th>
                                    <th>Địa chỉ thường trú</th>
                                    <th class="text-center">Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>`;

        tenants.forEach(t => {
            // Định dạng lại ngày sinh từ dữ liệu
            const dob = t.dob ? new Date(t.dob).toLocaleDateString('vi-VN') : '---';
            
            html += `
                <tr>
                    <td class="ps-3">
                        <div class="fw-bold text-dark">${t.fullName}</div>
                        <small class="text-muted"><i class="bi bi-calendar-event me-1"></i>NS: ${dob}</small>
                    </td>
                    <td>
                        <div><i class="bi bi-telephone me-2 text-primary"></i>${t.phoneNumber}</div>
                        <div class="small text-muted"><i class="bi bi-envelope me-2"></i>${t.email}</div>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border"><i class="bi bi-card-checklist me-1 text-secondary"></i>${t.idCardNumber}</span>
                    </td>
                    <td class="small text-truncate" style="max-width: 200px;" title="${t.permanentAddress}">
                        ${t.permanentAddress}
                    </td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-outline-warning btn-sm" onclick="editTenant('${t.id}')" title="Sửa"><i class="bi bi-pencil"></i></button>
                            <button class="btn btn-outline-danger btn-sm" onclick="deleteTenant('${t.id}', '${t.fullName}')" title="Xóa"><i class="bi bi-trash"></i></button>
                        </div>
                    </td>
                </tr>`;
        });

        html += `</tbody></table></div></div></div>`;
        container.innerHTML = html;

    } catch (error) {
        container.innerHTML = `
            <div class="alert alert-warning border-start border-4">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                Không thể lấy dữ liệu người thuê. Vui lòng kiểm tra API tại <strong>/api/Tenants</strong>
            </div>`;
        console.error("Lỗi Tenant API:", error);
    }
}
function addNewTenant() {
    const container = document.getElementById('room-container');
    document.querySelector('h2.mt-4').innerText = "Thêm Khách Thuê Mới";

    container.innerHTML = `
        <div class="card shadow border-0" style="max-width: 800px; margin: auto;">
            <div class="card-header bg-primary text-white">
                <h5 class="mb-0"><i class="bi bi-person-plus-fill me-2"></i>Nhập thông tin khách hàng</h5>
            </div>
            <div class="card-body">
                <form id="form-new-tenant">
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">HỌ VÀ TÊN</label>
                            <input type="text" id="t-fullname" class="form-control" placeholder="Nguyễn Văn A" required>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">NGÀY SINH</label>
                            <input type="date" id="t-dob" class="form-control" required>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">SỐ CCCD/CMND</label>
                            <input type="text" id="t-idcard" class="form-control" placeholder="075..." required>
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label fw-bold small">SỐ ĐIỆN THOẠI</label>
                            <input type="tel" id="t-phone" class="form-control" placeholder="09..." required>
                        </div>
                    </div>
                    <div class="mb-3">
                        <label class="form-label fw-bold small">ĐỊA CHỈ EMAIL</label>
                        <input type="email" id="t-email" class="form-control" placeholder="example@gmail.com">
                    </div>
                    <div class="mb-4">
                        <label class="form-label fw-bold small">ĐỊA CHỈ THƯỜNG TRÚ</label>
                        <textarea id="t-address" class="form-control" rows="2" placeholder="Số nhà, đường, phường..."></textarea>
                    </div>
                    <div class="d-flex gap-2">
                        <button type="submit" class="btn btn-primary w-100 fw-bold">XÁC NHẬN THÊM MỚI</button>
                        <button type="button" class="btn btn-light w-100 fw-bold" onclick="loadTenants()">HỦY BỎ</button>
                    </div>
                </form>
            </div>
        </div>
    `;

    // Lắng nghe sự kiện Submit
    document.getElementById('form-new-tenant').addEventListener('submit', saveNewTenant);
}
async function saveNewTenant(e) {
    e.preventDefault(); // Ngăn trang web load lại

    const tenantData = {
        fullName: document.getElementById('t-fullname').value,
        dob: document.getElementById('t-dob').value,
        idCardNumber: document.getElementById('t-idcard').value,
        phoneNumber: document.getElementById('t-phone').value,
        email: document.getElementById('t-email').value,
        permanentAddress: document.getElementById('t-address').value,
        createdAt: new Date().toISOString() // Lưu thời điểm tạo
    };

    try {
        const res = await fetch("https://localhost:7209/api/Tenants", {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(tenantData)
        });

        if (res.ok) {
            alert("Thêm khách thuê thành công!");
            loadTenants(); // Quay lại danh sách
        } else {
            const err = await res.json();
            console.error("Lỗi API:", err);
            alert("Lỗi: " + (err.title || "Không thể lưu thông tin khách"));
        }
    } catch (error) {
        console.error("Lỗi kết nối:", error);
        alert("Không thể kết nối đến máy chủ API.");
    }
}
// js/tenant.js

async function editTenant(id) {
    const container = document.getElementById('room-container');
    container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-warning"></div> Đang lấy thông tin khách hàng...</div>`;

    try {
        const res = await fetch(`https://localhost:7209/api/Tenants/${id}`);
        const t = await res.json();

        // Chuyển đổi định dạng ngày 2003-10-20T... thành 2003-10-20 để input date hiểu được
        const formattedDob = t.dob ? t.dob.split('T')[0] : "";

        document.querySelector('h2.mt-4').innerText = "Chỉnh Sửa Thông Tin Khách Thuê";
        container.innerHTML = `
            <div class="card shadow border-0" style="max-width: 800px; margin: auto;">
                <div class="card-header bg-warning text-dark">
                    <h5 class="mb-0"><i class="bi bi-pencil-square me-2"></i>Cập nhật thông tin: ${t.fullName}</h5>
                </div>
                <div class="card-body">
                    <form id="form-edit-tenant">
                        <input type="hidden" id="edit-t-id" value="${t.id}">
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">HỌ VÀ TÊN</label>
                                <input type="text" id="edit-t-fullname" class="form-control" value="${t.fullName}" required>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">NGÀY SINH</label>
                                <input type="date" id="edit-t-dob" class="form-control" value="${formattedDob}" required>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">SỐ CCCD/CMND</label>
                                <input type="text" id="edit-t-idcard" class="form-control" value="${t.idCardNumber}" required>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label fw-bold small">SỐ ĐIỆN THOẠI</label>
                                <input type="tel" id="edit-t-phone" class="form-control" value="${t.phoneNumber}" required>
                            </div>
                        </div>
                        <div class="mb-3">
                            <label class="form-label fw-bold small">ĐỊA CHỈ EMAIL</label>
                            <input type="email" id="edit-t-email" class="form-control" value="${t.email}">
                        </div>
                        <div class="mb-4">
                            <label class="form-label fw-bold small">ĐỊA CHỈ THƯỜNG TRÚ</label>
                            <textarea id="edit-t-address" class="form-control" rows="2">${t.permanentAddress}</textarea>
                        </div>
                        <div class="d-flex gap-2">
                            <button type="submit" class="btn btn-warning w-100 fw-bold">LƯU THAY ĐỔI</button>
                            <button type="button" class="btn btn-light w-100 fw-bold" onclick="loadTenants()">HỦY BỎ</button>
                        </div>
                    </form>
                </div>
            </div>
        `;

        document.getElementById('form-edit-tenant').addEventListener('submit', updateTenant);
    } catch (error) {
        alert("Không thể lấy thông tin khách thuê!");
        loadTenants();
    }
}
async function updateTenant(e) {
    e.preventDefault();
    const id = document.getElementById('edit-t-id').value;

    const updatedData = {
        id: id,
        fullName: document.getElementById('edit-t-fullname').value,
        dob: document.getElementById('edit-t-dob').value,
        idCardNumber: document.getElementById('edit-t-idcard').value,
        phoneNumber: document.getElementById('edit-t-phone').value,
        email: document.getElementById('edit-t-email').value,
        permanentAddress: document.getElementById('edit-t-address').value,
        updatedAt: new Date().toISOString() // Đánh dấu thời điểm cập nhật
    };

    try {
        const res = await fetch(`https://localhost:7209/api/Tenants/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(updatedData)
        });

        if (res.ok) {
            alert("Cập nhật thông tin khách thuê thành công!");
            loadTenants();
        } else {
            alert("Cập nhật thất bại, vui lòng kiểm tra lại dữ liệu!");
        }
    } catch (error) {
        console.error("Lỗi Update:", error);
    }
}
// js/tenant.js

async function deleteTenant(id, name) {
    // Hiển thị xác nhận với tên khách hàng để tránh nhầm lẫn
    const confirmDelete = confirm(`Bạn có chắc chắn muốn xóa khách thuê: ${name}?\nLưu ý: Hành động này không thể hoàn tác!`);

    if (confirmDelete) {
        try {
            const res = await fetch(`https://localhost:7209/api/Tenants/${id}`, {
                method: 'DELETE'
            });

            if (res.ok) {
                alert("Đã xóa khách thuê thành công!");
                loadTenants(); // Tải lại danh sách sau khi xóa
            } else {
                // Một số API sẽ chặn xóa nếu khách này đang có hợp đồng
                const err = await res.json();
                alert("Lỗi: " + (err.message || "Không thể xóa khách thuê này (có thể do ràng buộc dữ liệu)."));
            }
        } catch (error) {
            console.error("Lỗi khi xóa:", error);
            alert("Không thể kết nối đến máy chủ để thực hiện lệnh xóa.");
        }
    }
}