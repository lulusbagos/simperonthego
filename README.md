# Simper On The Go

Platform ujian SIMPER berbasis ASP.NET Core untuk alur teori + praktek, monitoring operasional, ringkasan hasil, dan sertifikasi kepatuhan ketentuan jalan hauling.

## Fitur Utama

- Portal peserta berbasis `Ref ID` untuk akses sesi ujian.
- Dashboard admin premium untuk monitoring ujian aktif, status kamera, dan hasil akhir.
- Manajemen master data: peserta, unit, bank soal, instruktur, user.
- Penilaian praktek terstruktur berbasis template item dan scoring.
- Summary Score resmi + PDF + QR verifikasi publik.
- Halaman Ketentuan Jalan Hauling dengan:
  - pembacaan bertahap per section,
  - quiz verifikasi acak (2 dari 10 bank soal),
  - persetujuan final,
  - update status `ketentuan_hauling` pada `tb_simper`,
  - sertifikat completion + QR + download PDF.
- UI responsif dengan tema premium dan animasi ringan.

## Stack Teknologi

- .NET 8 (ASP.NET Core MVC)
- Entity Framework Core + PostgreSQL (Npgsql)
- QuestPDF (dokumen PDF)
- QRCoder (QR code)
- Bootstrap + AdminLTE + CSS custom premium

## Struktur Project

- `Controllers/` : endpoint web (admin, exam, permit, account)
- `Application/` : service dan DTO aplikasi
- `Domain/Entities/` : model domain dan view entities
- `Infrastructure/Data/` : `ApplicationDbContext`
- `Infrastructure/Documents/` : generator PDF
- `Views/` : Razor view
- `wwwroot/` : asset statik (CSS, JS, image)

## Konfigurasi

Gunakan `appsettings.json` untuk environment utama, lalu override lokal pada `appsettings.Development.json` atau `appsettings.Local.json`.

Contoh connection string dapat dilihat di file:

- `appsettings.example.json`

## Menjalankan Aplikasi

```bash
dotnet restore
dotnet build
dotnet run
```

Default URL lokal:

- `http://localhost:5088`

## Kredensial Default (Development)

- Username: `admin`
- Password: `admin`

## Publish

```bash
dotnet publish -c Release -o D:\Publish\SimperOnTheGo
```

## Catatan

Project ini aktif dikembangkan untuk kebutuhan operasional internal SIMPER dan penyesuaian workflow di lapangan.
