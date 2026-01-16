import psycopg2
import json
import sys

print("Connecting to Supabase...", flush=True)

try:
    # Connect to Supabase with timeout
    conn = psycopg2.connect(
        host='db.khcjvtdgusteonzliyiv.supabase.co',
        port=6543,
        user='postgres',
        password='VfaezQqaKNaUaRa6',
        database='postgres',
        connect_timeout=10
    )
    print("Connected successfully!", flush=True)
except Exception as e:
    print(f"Connection failed: {e}", flush=True)
    sys.exit(1)

cur = conn.cursor()

try:
    # Get sample records
    print("Counting records...", flush=True)
    cur.execute("SELECT COUNT(*) FROM public.codex")
    count = cur.fetchone()[0]
    print(f"Total records in codex table: {count}\n", flush=True)

    print("Fetching sample records...", flush=True)
    cur.execute("SELECT guid, section, data FROM public.codex LIMIT 5")
    rows = cur.fetchall()
    print(f"Fetched {len(rows)} rows\n", flush=True)

    print("Sample Records:")
    print("=" * 80)

    for row in rows:
        guid, section, data = row
        print(f"\nGUID: {guid}")
        print(f"Section: {section}")
        print(f"Data: {json.dumps(data, indent=2)}")
        print("-" * 80)

    # Get unique sections
    print("\nFetching unique sections...", flush=True)
    cur.execute("SELECT DISTINCT section FROM public.codex ORDER BY section")
    sections = cur.fetchall()
    print("\n\nUnique Sections:")
    print("=" * 80)
    for section in sections:
        cur.execute("SELECT COUNT(*) FROM public.codex WHERE section = %s", (section[0],))
        count = cur.fetchone()[0]
        print(f"{section[0]}: {count} entries")

except Exception as e:
    print(f"Error during query: {e}", flush=True)
    import traceback
    traceback.print_exc()
finally:
    cur.close()
    conn.close()
    print("\nConnection closed.", flush=True)
