import requests
import json

# Supabase credentials
SUPABASE_URL = 'https://khcjvtdgusteonzliyiv.supabase.co'
SUPABASE_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtoY2p2dGRndXN0ZW9uemxpeWl2Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2ODEzMjQ5NywiZXhwIjoyMDgzNzA4NDk3fQ.xdRgQ1ZZotEOGe1p_k7YswB3lnZ3HW1CGJ7V0XYrZUE'

headers = {
    'apikey': SUPABASE_KEY,
    'Authorization': f'Bearer {SUPABASE_KEY}',
    'Content-Type': 'application/json'
}

print("Fetching data from Supabase...")

# Get count
response = requests.get(
    f'{SUPABASE_URL}/rest/v1/codex?select=count',
    headers=headers
)
print(f"Count response status: {response.status_code}")

# Get sample records
response = requests.get(
    f'{SUPABASE_URL}/rest/v1/codex?limit=5',
    headers=headers
)

if response.status_code == 200:
    data = response.json()
    print(f"\nTotal records fetched: {len(data)}\n")
    
    print("Sample Records:")
    print("=" * 80)
    
    for record in data:
        print(f"\nGUID: {record.get('guid')}")
        print(f"Section: {record.get('section')}")
        print(f"Data: {json.dumps(record.get('data'), indent=2)}")
        print("-" * 80)
    
    # Get unique sections
    response = requests.get(
        f'{SUPABASE_URL}/rest/v1/codex?select=section',
        headers=headers
    )
    
    if response.status_code == 200:
        all_records = response.json()
        sections = {}
        for record in all_records:
            section = record.get('section')
            sections[section] = sections.get(section, 0) + 1
        
        print("\n\nUnique Sections:")
        print("=" * 80)
        for section, count in sorted(sections.items()):
            print(f"{section}: {count} entries")
else:
    print(f"Error: {response.status_code}")
    print(response.text)
