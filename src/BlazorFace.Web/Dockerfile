#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BlazorFace.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/DirectoryListingSourceGenerator/DirectoryListingSourceGenerator.csproj", "src/DirectoryListingSourceGenerator/"]
COPY ["src/BlazorFace/BlazorFace.csproj", "src/BlazorFace/"]
COPY ["src/BlazorFace.Web/BlazorFace.Web.csproj", "src/BlazorFace.Web/"]
COPY ["src/FaceAiSharp/FaceAiSharp.csproj", "src/FaceAiSharp/"]
COPY ["src/FaceAiSharp.Bundle/FaceAiSharp.Bundle.csproj", "src/FaceAiSharp.Bundle/"]
RUN dotnet restore "src/BlazorFace.Web/BlazorFace.Web.csproj" -p:NoAndroid=TRUE
COPY . .
RUN dotnet build "src/BlazorFace.Web/BlazorFace.Web.csproj" -p:NoAndroid=TRUE --no-restore -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/BlazorFace.Web/BlazorFace.Web.csproj" -p:NoAndroid=TRUE --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorFace.Web.dll"]
