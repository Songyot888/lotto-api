# Stage 1: Build the application
# ใช้ .NET SDK image ซึ่งมีเครื่องมือครบสำหรับ build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ไฟล์ .csproj และทำการ restore package ก่อน
# เพื่อใช้ประโยชน์จาก Docker cache layer ทำให้ build ครั้งต่อไปเร็วขึ้น
COPY ["lotto_api.csproj", "./"]
RUN dotnet restore "./lotto_api.csproj"

# Copy โค้ดที่เหลือทั้งหมดเข้ามา
COPY . .

# สั่ง publish แอปพลิเคชันในโหมด Release ไปที่โฟลเดอร์ /app/publish
RUN dotnet publish "lotto_api.csproj" -c Release -o /app/publish

# Stage 2: Create the final runtime image
# ใช้ ASP.NET runtime image ซึ่งมีขนาดเล็กกว่ามาก
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy เฉพาะผลลัพธ์ที่ได้จากการ publish จาก stage แรก (build) มาใส่
COPY --from=build /app/publish .

# บอกให้ Docker รู้ว่าแอปพลิเคชันของเราทำงานที่ port 80 ภายใน container
EXPOSE 80

# คำสั่งสำหรับรันแอปพลิเคชันเมื่อ container เริ่มทำงาน
ENTRYPOINT ["dotnet", "lotto_api.dll"]