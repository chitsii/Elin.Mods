import os

def to_lf(path):
    extensions = ['.cs', '.xml', '.json', '.md', '.txt', '.csproj', '.sln', '.py', '.bat']
    count = 0
    for root, dirs, files in os.walk(path):
        if '.git' in dirs:
            dirs.remove('.git')
        if 'bin' in dirs:
            dirs.remove('bin')
        if 'obj' in dirs:
            dirs.remove('obj')

        for file in files:
            if any(file.endswith(ext) for ext in extensions):
                file_path = os.path.join(root, file)
                try:
                    with open(file_path, 'rb') as f:
                        content = f.read()

                    if b'\r\n' in content:
                        content = content.replace(b'\r\n', b'\n')
                        with open(file_path, 'wb') as f:
                            f.write(content)
                        print(f"Converted: {file_path}")
                        count += 1
                except Exception as e:
                    print(f"Error processing {file_path}: {e}")
    print(f"Total files converted: {count}")

if __name__ == "__main__":
    target_dir = os.path.dirname(os.path.abspath(__file__))
    print(f"Target directory: {target_dir}")
    to_lf(target_dir)
