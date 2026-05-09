using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using ApiQuanLyPhongTro.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ApiQuanLyPhongTro.Infrastructure.Data;

using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Đăng ký Generic Repository cho TẤT CẢ các bảng (Người thuê, Hợp đồng, Tòa nhà...)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Chặn vòng lặp vô tận khi trả về JSON     
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
// Đăng ký riêng cho Invoice vì nó có báo cáo
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChoPhepTatCa", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("ChoPhepTatCa");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
