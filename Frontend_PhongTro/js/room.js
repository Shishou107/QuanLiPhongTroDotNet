// 1. Khai báo cấu hình chung
const API_URL = "https://localhost:7209/api/Rooms";

// 2. Logic nút Toggle Menu (Sidebar)
window.addEventListener('DOMContentLoaded', event => {
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');
            localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
        });
    }
});

// ==========================================
// 3. HÀM CHÍNH: TẢI DANH SÁCH PHÒNG
// ==========================================
async function loadRooms() {
    const container = document.getElementById('room-container');
    
    // Tạo phần Header chứa tiêu đề và nút Thêm mới
    container.innerHTML = `
        <div class="col-12 mb-4 d-flex justify-content-between align-items-center">
            
            <button class="btn btn-primary fw-bold shadow-sm" onclick="showAddRoomModal()">
                <i class="bi bi-plus-lg"></i> Thêm phòng mới
            </button>
        </div>
        <div id="rooms-list-area" class="row"> 
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
                <p class="mt-2">Đang tải dữ liệu phòng...</p>
            </div>
        </div>
    `;

    try {
        const response = await fetch(API_URL);
        const data = await response.json();
        const listArea = document.getElementById('rooms-list-area');
        listArea.innerHTML = ""; // Xóa spinner

        if (data.length === 0) {
            listArea.innerHTML = '<div class="alert alert-info text-center">Chưa có phòng nào. Hãy bấm "Thêm phòng mới"!</div>';
            return;
        }

        data.forEach(p => {
            const statusClass = p.status === 0 ? 'text-success' : (p.status === 1 ? 'text-danger' : 'text-warning');
            const statusName = p.status === 0 ? 'Trống' : (p.status === 1 ? 'Đã thuê' : 'Bảo trì');
            const price = new Intl.NumberFormat('vi-VN').format(p.basePrice);

            listArea.innerHTML += `
                <div class="col-md-4 col-xl-3 mb-4">
                    <div class="room-card shadow-sm border p-3 rounded bg-white h-100">
                        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-2">
                            <strong class="text-uppercase text-dark">Phòng ${p.roomNumber}</strong>
                            <span class="small fw-bold ${statusClass}">${statusName}</span>
                        </div>
                        <div class="small text-muted mb-1"><i class="bi bi-rulers"></i> Diện tích: ${p.area} m²</div>
                        <div class="small text-muted mb-3 text-truncate"><i class="bi bi-people"></i> Sức chứa: ${p.capacity} người</div>
                        <div class="d-flex justify-content-between align-items-center mt-auto">
                            <span class="text-primary fw-bold">${price} đ</span>
                            <button class="btn btn-sm btn-outline-primary" onclick="showDetail('${p.id}')">Chi tiết</button>
                        </div>
                    </div>
                </div>`;
        });
    } catch (error) {
        console.error("Lỗi API:", error);
        document.getElementById('rooms-list-area').innerHTML = 
            '<div class="alert alert-danger mx-3">Lỗi kết nối API. Hãy kiểm tra Backend!</div>';
    }
}

// Gọi hàm tải dữ liệu khi trang web sẵn sàng
loadRooms();

// ==========================================
// 4. HÀM HIỂN THỊ CHI TIẾT PHÒNG (MODAL)
// ==========================================
async function showDetail(roomId) {
    const content = document.getElementById('modal-content');
    content.innerHTML = '<div class="text-center p-5"><div class="spinner-border text-primary"></div></div>';
    
    const myModal = new bootstrap.Modal(document.getElementById('roomModal'));
    myModal.show();

    try {
        const res = await fetch(`${API_URL}/${roomId}`);
        const room = await res.json();

        content.innerHTML = `
            <div class="text-center mb-3">
                <img src="${room.imageUrl}" class="img-fluid rounded shadow-sm border" 
                     style="max-height: 250px; width: 100%; object-fit: cover;"
                     onerror="this.src='https://placehold.co/600x400?text=No+Image'">
            </div>
            <div class="px-2">
                <h4 class="fw-bold text-primary mb-3">Thông tin Phòng ${room.roomNumber}</h4>
                <div class="row mb-3">
                    <div class="col-6">
                        <p class="mb-0 text-muted small">DIỆN TÍCH</p>
                        <p class="fw-bold">${room.area} m²</p>
                    </div>
                    <div class="col-6">
                        <p class="mb-0 text-muted small">SỨC CHỨA</p>
                        <p class="fw-bold">${room.capacity} người</p>
                    </div>
                </div>
                <div class="mb-3">
                    <p class="mb-1 text-muted small">MÔ TẢ</p>
                    <p class="text-dark">${room.description || 'Chưa có mô tả.'}</p>
                </div>
                <div class="d-flex justify-content-between align-items-center bg-light p-3 rounded border mb-3">
                    <span class="fw-bold">Giá thuê hàng tháng:</span>
                    <span class="fs-4 fw-bold text-danger">${room.basePrice.toLocaleString('vi-VN')} đ</span>
                </div>
                <div class="row g-2">
                    <div class="col-6">
                        <button class="btn btn-warning w-100 fw-bold" onclick="editRoom('${room.id}')">Sửa</button>
                    </div>
                    <div class="col-6">
                        <button class="btn btn-danger w-100 fw-bold" onclick="deleteRoom('${room.id}', '${room.roomNumber}')">Xóa</button>
                    </div>
                </div>
            </div>`;
    } catch (error) {
        content.innerHTML = '<div class="alert alert-danger">Lỗi tải dữ liệu chi tiết!</div>';
    }
}

// ==========================================
// 5. HÀM THÊM PHÒNG MỚI
// ==========================================
function showAddRoomModal() {
    const content = document.getElementById('modal-content');
    const myModal = new bootstrap.Modal(document.getElementById('roomModal'));
    
    content.innerHTML = `
        <div class="p-2">
            <h5 class="fw-bold mb-3 text-primary"><i class="bi bi-plus-circle me-2"></i>Tạo phòng mới</h5>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="small fw-bold">SỐ PHÒNG</label>
                    <input type="text" id="add-roomNumber" class="form-control" placeholder="101">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="small fw-bold">GIÁ THUÊ (VNĐ)</label>
                    <input type="number" id="add-basePrice" class="form-control" placeholder="3000000">
                </div>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="small fw-bold">DIỆN TÍCH (m²)</label>
                    <input type="number" id="add-area" class="form-control" value="20">
                </div>
                <div class="col-md-6 mb-3">
                    <label class="small fw-bold">SỨC CHỨA</label>
                    <input type="number" id="add-capacity" class="form-control" value="2">
                </div>
            </div>
            <div class="mb-3">
                <label class="small fw-bold">ID TÒA NHÀ</label>
                <input type="text" id="add-buildingId" class="form-control" value="3fa85f64-5717-4562-b3fc-2c963f66afa6">
            </div>
            <div class="mb-3">
                <label class="small fw-bold">MÔ TẢ</label>
                <textarea id="add-description" class="form-control" rows="2"></textarea>
            </div>
            <button class="btn btn-success w-100 fw-bold py-2" onclick="saveNewRoom()">XÁC NHẬN TẠO</button>
        </div>`;
    myModal.show();
}

async function saveNewRoom() {
    const roomData = {
        id: "00000000-0000-0000-0000-000000000000",
        roomNumber: document.getElementById('add-roomNumber').value,
        basePrice: parseFloat(document.getElementById('add-basePrice').value) || 0,
        area: parseFloat(document.getElementById('add-area').value) || 0,
        capacity: parseInt(document.getElementById('add-capacity').value) || 1,
        description: document.getElementById('add-description').value,
        buildingId: document.getElementById('add-buildingId').value,
        status: 0,
        createdAt: new Date().toISOString()
    };

    if (!roomData.roomNumber || roomData.basePrice <= 0) {
        alert("Vui lòng nhập đầy đủ Số phòng và Giá thuê!");
        return;
    }

    try {
        const res = await fetch(API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(roomData)
        });

        if (res.ok) {
            alert("Tạo phòng mới thành công!");
            location.reload();
        } else {
            const err = await res.text();
            alert("Lỗi: " + err);
        }
    } catch (error) {
        alert("Lỗi kết nối máy chủ!");
    }
}

// ==========================================
// 6. HÀM CHỈNH SỬA & XÓA
// ==========================================
function editRoom(id) {
    const content = document.getElementById('modal-content');
    fetch(`${API_URL}/${id}`)
        .then(res => res.json())
        .then(room => {
            content.innerHTML = `
                <div class="p-2">
                    <h5 class="fw-bold mb-3">Chỉnh sửa phòng ${room.roomNumber}</h5>
                    <div class="mb-3"><label class="small fw-bold">SỐ PHÒNG</label><input type="text" id="edit-roomNumber" class="form-control" value="${room.roomNumber}"></div>
                    <div class="mb-3"><label class="small fw-bold">GIÁ THUÊ</label><input type="number" id="edit-basePrice" class="form-control" value="${room.basePrice}"></div>
                    <div class="mb-3">
                        <label class="small fw-bold">TRẠNG THÁI</label>
                        <select id="edit-status" class="form-select">
                            <option value="0" ${room.status === 0 ? 'selected' : ''}>Trống</option>
                            <option value="1" ${room.status === 1 ? 'selected' : ''}>Đang thuê</option>
                            <option value="2" ${room.status === 2 ? 'selected' : ''}>Bảo trì</option>
                        </select>
                    </div>
                    <button class="btn btn-primary w-100" onclick="saveEditRoom('${id}')">LƯU THAY ĐỔI</button>
                </div>`;
        });
}

async function saveEditRoom(id) {
    const resOld = await fetch(`${API_URL}/${id}`);
    const oldRoom = await resOld.json();

    const updatedData = {
        ...oldRoom,
        roomNumber: document.getElementById('edit-roomNumber').value,
        basePrice: parseFloat(document.getElementById('edit-basePrice').value),
        status: parseInt(document.getElementById('edit-status').value),
        updatedAt: new Date().toISOString()
    };

    const res = await fetch(`${API_URL}/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(updatedData)
    });

    if (res.ok) {
        alert("Cập nhật thành công!");
        location.reload();
    }
}

async function deleteRoom(id, roomNumber) {
    if (confirm(`Xóa phòng ${roomNumber}?`)) {
        const res = await fetch(`${API_URL}/${id}`, { method: 'DELETE' });
        if (res.ok) { location.reload(); }
    }
}