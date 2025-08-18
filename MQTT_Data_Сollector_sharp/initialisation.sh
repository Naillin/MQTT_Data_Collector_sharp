#!/bin/bash

# Проверяем наличие sqlite3
if ! command -v sqlite3 &> /dev/null; then
    echo "Ошибка: sqlite3 не установлен. Установите его перед запуском скрипта."
    echo "Для Ubuntu/Debian: sudo apt install sqlite3"
    echo "Для CentOS/RHEL: sudo yum install sqlite"
    exit 1
fi

# Проверяем наличие SQL-файлов
if [ ! -f "mqtt_data.sql" ]; then
    echo "Ошибка: файл mqtt_data.sql не найден в текущей директории."
    exit 1
fi

if [ ! -f "mqtt_test.sql" ]; then
    echo "Ошибка: файл mqtt_test.sql не найден в текущей директории."
    exit 1
fi

# Удаляем старую БД, если она существует
if [ -f "mqtt_data.db" ]; then
    rm mqtt_data.db
    echo "Старая база данных mqtt_data.db удалена."
fi

# Создаем новую БД и импортируем данные
echo "Создание новой базы данных mqtt_data.db..."
sqlite3 mqtt_data.db < mqtt_data.sql

echo "Добавление тестовых данных..."
sqlite3 mqtt_data.db < mqtt_test.sql

# Устанавливаем права доступа
echo "Установка прав доступа..."
chmod 664 mqtt_data.db  # Чтение и запись для владельца и группы, только чтение для остальных
chown $USER:$USER mqtt_data.db  # Владелец - текущий пользователь

echo "Готово! База данных mqtt_data.db успешно создана и настроена."
