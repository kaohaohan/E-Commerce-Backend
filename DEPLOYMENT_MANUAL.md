# 🚀 E-Commerce Backend 部署手冊

> 這份文檔包含所有手動操作的指令，即使沒有 AI 協助也能自己部署！

---

## 📋 目錄

1. [本地開發環境啟動](#本地開發環境啟動)
2. [Docker 操作](#docker-操作)
3. [AWS CLI 配置](#aws-cli-配置)
4. [AWS ECR 推送鏡像](#aws-ecr-推送鏡像)
5. [常見問題排查](#常見問題排查)

---

## 🔑 我的 AWS 資訊

```
AWS 帳號 ID: 585548213310
IAM 用戶名: deploy-user
Region: us-east-1
Access Key 文件: ~/Downloads/deploy-user_accessKeys.csv
```

---

## 本地開發環境啟動

### 1. 啟動 SQL Server (Docker)

```bash
# 查看 SQL Server 容器狀態
docker ps -a | grep sqlserver

# 如果容器已停止，啟動它
docker start sqlserver

# 確認運行狀態
docker ps
```

### 2. 啟動 Redis (Docker)

```bash
# 查看 Redis 容器狀態
docker ps -a | grep redis

# 如果容器已停止，啟動它
docker start redis

# 確認運行狀態
docker ps
```

### 3. 啟動後端 API

```bash
# 進入專案目錄
cd /Users/haohan/E-Commerce-Backend

# 運行後端（開發模式）
dotnet run --urls "http://localhost:5000"

# 或者在背景運行
dotnet run --urls "http://localhost:5000" > /tmp/backend.log 2>&1 &
```

### 4. 測試 API

```bash
# 測試健康檢查
curl http://localhost:5000/api/products/health

# 測試商品查詢（Contains）
curl "http://localhost:5000/api/products/search-contains?name=全聯"

# 測試商品查詢（StartsWith）
curl "http://localhost:5000/api/products/search-starts-with?name=全聯"

# 測試快取查詢（Cached）
curl "http://localhost:5000/api/products/search-cached?name=全聯"
```

---

## 🐳 Docker 操作

### 構建專案的 Docker 鏡像

```bash
# 進入專案目錄
cd /Users/haohan/E-Commerce-Backend

# 構建鏡像（標籤：ecommerce-backend）
docker build -t ecommerce-backend .

# 查看構建好的鏡像
docker images | grep ecommerce-backend
```

### 本地運行 Docker 容器

```bash
# 運行容器（映射端口 5000）
docker run -d \
  --name ecommerce-api \
  -p 5000:5000 \
  ecommerce-backend

# 查看運行日誌
docker logs ecommerce-api

# 進入容器內部
docker exec -it ecommerce-api /bin/bash

# 停止容器
docker stop ecommerce-api

# 刪除容器
docker rm ecommerce-api
```

### 常用 Docker 指令

```bash
# 查看所有容器（包含停止的）
docker ps -a

# 查看所有鏡像
docker images

# 刪除指定鏡像
docker rmi 鏡像ID

# 清理未使用的容器
docker container prune

# 清理未使用的鏡像
docker image prune

# 查看 Docker 資源使用
docker system df
```

---

## ☁️ AWS CLI 配置

### 初次配置 AWS CLI

```bash
# 方法 1：交互式配置
aws configure

# 會詢問：
# AWS Access Key ID: 從 CSV 文件複製
# AWS Secret Access Key: 從 CSV 文件複製
# Default region name: us-east-1
# Default output format: json
```

```bash
# 方法 2：直接設定（使用你的真實密鑰）
aws configure set aws_access_key_id AKIAYQVLEVQ7KOKWW6M6
aws configure set aws_secret_access_key 你的Secret_Key
aws configure set region us-east-1
aws configure set output json
```

### 檢查配置

```bash
# 查看當前配置
aws configure list

# 測試連線（獲取用戶身份）
aws sts get-caller-identity

# 應該會返回：
# {
#     "UserId": "...",
#     "Account": "585548213310",
#     "Arn": "arn:aws:iam::585548213310:user/deploy-user"
# }
```

---

## 📦 AWS ECR 推送鏡像

### 1. 創建 ECR Repository（只需一次）

```bash
# 創建名為 ecommerce-backend 的倉庫
aws ecr create-repository \
  --repository-name ecommerce-backend \
  --region us-east-1

# 查看所有 ECR repositories
aws ecr describe-repositories --region us-east-1
```

### 2. 登入 ECR

```bash
# 獲取登入密碼並登入（每次推送前執行）
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  585548213310.dkr.ecr.us-east-1.amazonaws.com
```

### 3. 標記並推送鏡像

```bash
# 標記本地鏡像（準備推送到 ECR）
docker tag ecommerce-backend:latest \
  585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

# 推送鏡像到 ECR
docker push 585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

# 查看推送的鏡像
aws ecr describe-images \
  --repository-name ecommerce-backend \
  --region us-east-1
```

### 完整流程（一鍵執行）

```bash
# 從構建到推送的完整流程
cd /Users/haohan/E-Commerce-Backend

# 1. 構建鏡像
docker build -t ecommerce-backend .

# 2. 登入 ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  585548213310.dkr.ecr.us-east-1.amazonaws.com

# 3. 標記鏡像
docker tag ecommerce-backend:latest \
  585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

# 4. 推送鏡像
docker push 585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

echo "✅ 推送完成！"
```

---

## 🔧 常見問題排查

### 問題 1：AWS CLI 找不到憑證

```bash
# 檢查配置文件是否存在
ls -la ~/.aws/

# 查看配置內容
cat ~/.aws/config
cat ~/.aws/credentials

# 重新配置
aws configure
```

### 問題 2：Docker login 失敗

```bash
# 確認 AWS CLI 配置正確
aws sts get-caller-identity

# 確認有 ECR 權限
aws ecr describe-repositories --region us-east-1

# 重新登入
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  585548213310.dkr.ecr.us-east-1.amazonaws.com
```

### 問題 3：Docker 構建失敗

```bash
# 檢查 Dockerfile 是否存在
ls -la Dockerfile

# 查看詳細錯誤訊息
docker build -t ecommerce-backend . --no-cache

# 清理 Docker 快取
docker builder prune
```

### 問題 4：容器運行但無法訪問

```bash
# 檢查容器是否運行
docker ps

# 查看容器日誌
docker logs ecommerce-api

# 檢查端口是否被佔用
lsof -i :5000

# 測試容器內部網絡
docker exec ecommerce-api curl localhost:5000/api/products/health
```

### 問題 5：SQL Server 連線失敗

```bash
# 檢查 SQL Server 是否運行
docker ps | grep sqlserver

# 啟動 SQL Server
docker start sqlserver

# 檢查連線字串（在 appsettings.json）
cat appsettings.json | grep ConnectionStrings
```

---

## 🎯 快速啟動腳本

### 本地開發環境一鍵啟動

```bash
#!/bin/bash

echo "🚀 啟動 E-Commerce Backend 開發環境..."

# 1. 啟動 SQL Server
echo "📦 啟動 SQL Server..."
docker start sqlserver

# 2. 啟動 Redis
echo "📦 啟動 Redis..."
docker start redis

# 等待服務啟動
sleep 3

# 3. 啟動後端 API
echo "🔥 啟動後端 API..."
cd /Users/haohan/E-Commerce-Backend
dotnet run --urls "http://localhost:5000"
```

### ECR 推送一鍵腳本

```bash
#!/bin/bash

echo "🚀 開始推送到 AWS ECR..."

cd /Users/haohan/E-Commerce-Backend

# 1. 構建鏡像
echo "📦 構建 Docker 鏡像..."
docker build -t ecommerce-backend .

# 2. 登入 ECR
echo "🔐 登入 AWS ECR..."
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  585548213310.dkr.ecr.us-east-1.amazonaws.com

# 3. 標記鏡像
echo "🏷️  標記鏡像..."
docker tag ecommerce-backend:latest \
  585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

# 4. 推送鏡像
echo "⬆️  推送鏡像到 ECR..."
docker push 585548213310.dkr.ecr.us-east-1.amazonaws.com/ecommerce-backend:latest

echo "✅ 推送完成！"
```

---

## 📚 參考資源

### 官方文檔

- [AWS CLI 文檔](https://docs.aws.amazon.com/cli/)
- [AWS ECR 文檔](https://docs.aws.amazon.com/ecr/)
- [Docker 文檔](https://docs.docker.com/)
- [.NET 文檔](https://docs.microsoft.com/dotnet/)

### 常用命令速查

```bash
# 查看系統資源
docker system df
docker stats

# 查看網絡
docker network ls
docker network inspect bridge

# 查看卷
docker volume ls
docker volume inspect 卷名稱
```

---

## 🎤 面試時如何展示

### 1. 本地演示流程（2 分鐘）

```
1. 展示 demo.html 界面
2. 執行三種查詢方式，展示效能差異
3. 打開瀏覽器開發者工具，展示請求時間
```

### 2. 架構講解要點

```
✅ 使用 .NET 8 + ASP.NET Core Web API
✅ SQL Server 資料庫 + Connection Pooling
✅ Redis Cache-Aside Pattern
✅ Database Indexing (B-Tree)
✅ Docker 容器化
✅ AWS ECR + ECS 部署
✅ Application Load Balancer + Auto Scaling
```

### 3. 效能數據

```
📊 測試環境：
- 資料量：100,000 筆商品
- 併發：100 users
- 測試工具：JMeter

📈 效能結果：
- Contains（無索引）：~300ms
- StartsWith（有索引）：~30ms（提升 10x）
- Cached（Redis）：~3ms（提升 100x）
- 錯誤率：0%
```

---

## ✅ 部署檢查清單

### 部署前

- [ ] SQL Server 運行中
- [ ] Redis 運行中
- [ ] 本地測試通過
- [ ] Docker 鏡像構建成功
- [ ] AWS CLI 配置完成

### 部署中

- [ ] ECR Repository 已創建
- [ ] Docker 鏡像已推送到 ECR
- [ ] ECS Cluster 已創建
- [ ] Task Definition 已設定
- [ ] ALB 已配置

### 部署後

- [ ] API 可以訪問
- [ ] 健康檢查通過
- [ ] Auto Scaling 正常運作
- [ ] 監控和日誌正常

---

**最後更新：2025-11-17**

**作者：haohan920**
