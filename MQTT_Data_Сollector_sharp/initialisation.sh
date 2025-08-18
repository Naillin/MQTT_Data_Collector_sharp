#!/bin/bash

# ��������� ������� sqlite3
if ! command -v sqlite3 &> /dev/null; then
    echo "������: sqlite3 �� ����������. ���������� ��� ����� �������� �������."
    echo "��� Ubuntu/Debian: sudo apt install sqlite3"
    echo "��� CentOS/RHEL: sudo yum install sqlite"
    exit 1
fi

# ��������� ������� SQL-������
if [ ! -f "mqtt_data.sql" ]; then
    echo "������: ���� mqtt_data.sql �� ������ � ������� ����������."
    exit 1
fi

if [ ! -f "mqtt_test.sql" ]; then
    echo "������: ���� mqtt_test.sql �� ������ � ������� ����������."
    exit 1
fi

# ������� ������ ��, ���� ��� ����������
if [ -f "mqtt_data.db" ]; then
    rm mqtt_data.db
    echo "������ ���� ������ mqtt_data.db �������."
fi

# ������� ����� �� � ����������� ������
echo "�������� ����� ���� ������ mqtt_data.db..."
sqlite3 mqtt_data.db < mqtt_data.sql

echo "���������� �������� ������..."
sqlite3 mqtt_data.db < mqtt_test.sql

# ������������� ����� �������
echo "��������� ���� �������..."
chmod 664 mqtt_data.db  # ������ � ������ ��� ��������� � ������, ������ ������ ��� ���������
chown $USER:$USER mqtt_data.db  # �������� - ������� ������������

echo "������! ���� ������ mqtt_data.db ������� ������� � ���������."
