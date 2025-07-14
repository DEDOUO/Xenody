import pandas as pd
import os

def convert_sheet_format(input_path, output_path):
    # 读取所有sheet
    try:
        xls = pd.ExcelFile(input_path)
        sheet_names = xls.sheet_names
    except Exception as e:
        print(f"读取Excel失败：{e}")
        return

    # 处理tap、slide、flick三个sheet
    note_sheets = ['tap', 'slide', 'flick']
    converted_data = []
    
    for sheet_name in note_sheets:
        if sheet_name in sheet_names:
            df = xls.parse(sheet_name)
            if not df.empty:
                if sheet_name == 'tap':
                    df['Slide'] = 0
                    df['Dir'] = ''
                elif sheet_name == 'slide':
                    df['Slide'] = 1
                    df['Dir'] = ''
                elif sheet_name == 'flick':
                    df['Slide'] = 0
                # 确保列存在（避免flick缺少Dir列）
                if 'Dir' not in df.columns:
                    df['Dir'] = ''
                # 选择统一的列并添加到转换后的数据中
                converted_data.append(df[['startT', 'startX', 'Size', 'Pid', 'Slide', 'Dir']])
    
    # 合并三个sheet的数据
    if converted_data:
        df_combined = pd.concat(converted_data, ignore_index=True)
        # 按startT排序
        df_combined = df_combined.sort_values(by='startT').reset_index(drop=True)
    else:
        print("警告：未找到有效的tap、slide或flick数据！")
        df_combined = pd.DataFrame(columns=['startT', 'startX', 'Size', 'Pid', 'Slide', 'Dir'])

    # ----------------------
    # 保存为新Excel文件（保留其他sheet）
    # ----------------------
    try:
        with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
            # 写入合并后的tap sheet
            df_combined.to_excel(writer, sheet_name='tap', index=False)
            
            # 写入其他sheet（不包括原tap、slide、flick）
            for sheet_name in sheet_names:
                if sheet_name not in note_sheets:
                    df = xls.parse(sheet_name)
                    df.to_excel(writer, sheet_name=sheet_name, index=False)
            
        print(f"转换成功！新文件已保存至：{output_path}")
    except Exception as e:
        print(f"保存文件失败：{e}")

# ----------------------
# 脚本使用说明
# ----------------------
if __name__ == "__main__":
    # 输入文件路径（替换为你的Excel文件路径）
    input_excel = "E:/Xenody-main/Assets/StreamingAssets/Songs/Chart4.xlsx"  # 原Excel文件
    # 输出文件路径（转换后的新文件）
    output_excel = "E:/Xenody-main/Assets/StreamingAssets/Songs/Chart4New.xlsx"

    # 检查输入文件是否存在
    if not os.path.exists(input_excel):
        print(f"错误：未找到输入文件 {input_excel}，请检查路径是否正确。")
    else:
        convert_sheet_format(input_excel, output_excel)