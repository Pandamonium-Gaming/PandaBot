import psycopg2
import json

# Connect to Supabase
conn = psycopg2.connect(
    host='db.khcjvtdgusteonzliyiv.supabase.co',
    port=6543,
    user='postgres',
    password='VfaezQqaKNaUaRa6',
    database='postgres'
)

cur = conn.cursor()

# Get all tables
print("=" * 60)
print("TABLES IN DATABASE")
print("=" * 60)
cur.execute("""
    SELECT table_schema, table_name 
    FROM information_schema.tables 
    WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
    ORDER BY table_schema, table_name;
""")
tables = cur.fetchall()

for schema, table in tables:
    print(f"{schema}.{table}")
    
print("\n" + "=" * 60)
print("TABLE SCHEMAS")
print("=" * 60)

# Get schema for each table
for schema, table in tables:
    print(f"\n{schema}.{table}:")
    print("-" * 60)
    cur.execute("""
        SELECT 
            column_name, 
            data_type, 
            is_nullable,
            column_default
        FROM information_schema.columns 
        WHERE table_schema = %s AND table_name = %s
        ORDER BY ordinal_position;
    """, (schema, table))
    
    columns = cur.fetchall()
    for col_name, data_type, nullable, default in columns:
        null_str = "NULL" if nullable == "YES" else "NOT NULL"
        default_str = f" DEFAULT {default}" if default else ""
        print(f"  {col_name}: {data_type} {null_str}{default_str}")

cur.close()
conn.close()

print("\n" + "=" * 60)
print("Schema discovery complete!")
print("=" * 60)
