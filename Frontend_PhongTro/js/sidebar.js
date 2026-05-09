// js/sidebar.js
document.addEventListener('DOMContentLoaded', () => {
    // Định nghĩa danh sách menu
    const menus = {
        'menu-rooms': typeof loadRooms !== 'undefined' ? loadRooms : null,
        'menu-tenants': typeof loadTenants !== 'undefined' ? loadTenants : null,
        'menu-contracts': typeof loadContracts !== 'undefined' ? loadContracts : null,
        'menu-services': typeof loadServices !== 'undefined' ? loadServices : null
    };

    Object.keys(menus).forEach(id => {
        const element = document.getElementById(id);
        
        if (element) {
            element.addEventListener('click', (e) => {
                e.preventDefault();

                // 1. Xử lý UI: Đổi màu nút
                document.querySelectorAll('.list-group-item').forEach(link => {
                    link.classList.remove('active', 'bg-primary', 'text-white');
                });
                element.classList.add('active', 'bg-primary', 'text-white');

                // 2. Xử lý Logic: Gọi hàm an toàn
                const targetFunction = menus[id];
                
                if (typeof targetFunction === 'function') {
                    targetFunction();
                } else {
                    console.error(`Lỗi: Hàm xử lý cho ID "${id}" chưa được tải. Kiểm tra file JS tương ứng!`);
                    // Hiển thị thông báo nhẹ cho người dùng thấy
                    const container = document.getElementById('room-container');
                    if(container) {
                        container.innerHTML = `<div class="alert alert-warning">Tính năng này đang được cập nhật hoặc thiếu file script!</div>`;
                    }
                }
            });
        }
    });
});