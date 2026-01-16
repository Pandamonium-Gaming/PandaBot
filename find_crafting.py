import psycopg2
import json
import sys

print("Attempting to connect to Supabase...", flush=True)

try:
    # Try with IPv4 preference
    conn = psycopg2.connect(
        host='db.khcjvtdgusteonzliyiv.supabase.co',
        port=5432,  # Try standard PostgreSQL port instead of 6543
        user='postgres',
        password='VfaezQqaKNaUaRa6',
        database='postgres',
        connect_timeout=10
    )
    print("Connected successfully!", flush=True)
    
    cur = conn.cursor()
    
    # Find items with Artisanship.Crafting tag
    print("\nSearching for crafting recipes...", flush=True)
    query = """
        SELECT guid, section, data 
        FROM public.codex 
        WHERE section = 'items' 
        AND data->'gameplayTags' @> '["Artisanship.Crafting"]'::jsonb
        LIMIT 5
    """
    
    cur.execute(query)
    rows = cur.fetchall()
    
    print(f"\nFound {len(rows)} crafting recipe(s)\n")
    print("=" * 80)
    
    for row in rows:
        guid, section, data = row
        print(f"\nGUID: {guid}")
        print(f"Section: {section}")
        print(f"Item Name: {data.get('itemName', 'N/A')}")
        print(f"Internal Name: {data.get('name', 'N/A')}")
        print(f"Gameplay Tags: {data.get('gameplayTags', [])}")
        
        # Look for crafting-related fields
        print("\nCrafting-related fields:")
        for key in data.keys():
            if any(keyword in key.lower() for keyword in ['craft', 'recipe', 'material', 'component', 'ingredient', 'require', 'cost']):
                print(f"  {key}: {json.dumps(data[key], indent=4)}")
        
        print("\nFull data structure:")
        print(json.dumps(data, indent=2))
        print("-" * 80)
    
    # Also check what unique sections exist
    print("\n\nChecking all unique sections...", flush=True)
    cur.execute("SELECT DISTINCT section FROM public.codex ORDER BY section")
    sections = cur.fetchall()
    print("Unique sections found:")
    for section in sections:
        print(f"  - {section[0]}")
    
    cur.close()
    conn.close()
    
except psycopg2.OperationalError as e:
    print(f"Connection failed on port 5432: {e}", flush=True)
    print("\nTrying port 6543...", flush=True)
    
    try:
        conn = psycopg2.connect(
            host='db.khcjvtdgusteonzliyiv.supabase.co',
            port=6543,
            user='postgres',
            password='VfaezQqaKNaUaRa6',
            database='postgres',
            connect_timeout=10
        )
        print("Connected on port 6543!", flush=True)
        # Repeat the same queries...
        
    except Exception as e2:
        print(f"Connection also failed on port 6543: {e2}", flush=True)
        print("\n" + "=" * 80)
        print("CONNECTION FAILED")
        print("=" * 80)
        print("\nPlease run this query in your Supabase SQL Editor:\n")
        print("""
SELECT guid, section, data 
FROM public.codex 
WHERE section = 'items' 
AND data->'gameplayTags' @> '["Artisanship.Crafting"]'::jsonb
LIMIT 3;
        """)
        sys.exit(1)
        
except Exception as e:
    print(f"Unexpected error: {e}", flush=True)
    import traceback
    traceback.print_exc()
    sys.exit(1)
