import pandas as pd
import json
import numpy as np

excel_file = 'Chart示例.xlsx'

# 读取Excel文件的各个sheet
planes_df = pd.read_excel(excel_file, sheet_name='planes')
taps_df = pd.read_excel(excel_file, sheet_name='taps')
holds_df = pd.read_excel(excel_file, sheet_name='holds')
slides_df = pd.read_excel(excel_file, sheet_name='slides')
flicks_df = pd.read_excel(excel_file, sheet_name='flicks')
stars_df = pd.read_excel(excel_file, sheet_name='stars')

# 定义函数处理DataFrame，将坐标和时间戳保留三位小数
def process_dataframe(df):
    def convert_to_3_decimals(x):
        if isinstance(x, (np.float64, float)):
            return round(x, 3)
        return x

    # 找出需要处理的列
    timestamp_and_coordinate_cols = [col for col in df.columns if any(key in col for key in ['startT', 'startX', 'startY', 'endT', 'endX', 'endY'])]
    other_cols = [col for col in df.columns if col not in timestamp_and_coordinate_cols]

    # 对需要处理的列应用转换函数
    df_timestamp_and_coordinate = df[timestamp_and_coordinate_cols].apply(convert_to_3_decimals)
    df_other = df[other_cols]

    # 合并处理后的列
    return pd.concat([df_timestamp_and_coordinate, df_other], axis=1)

# 处理 planes 数据
planes_data = []
for _, group in planes_df.groupby('id'):
    plane = {
        'id': int(group['id'].iloc[0]),
        'color': str(group['color'].iloc[0]),  # 添加 color 信息
        'sub': process_dataframe(group[['startT', 'startY', 'endT', 'endY', 'Func']]).to_dict('records')
    }
    planes_data.append(plane)

# 处理 taps 数据
taps_data = process_dataframe(taps_df[['startT', 'startX', 'Size', 'Pid']]).to_dict('records')

# 处理 holds 数据
holds_data = []
for _, group in holds_df.groupby('id'):
    hold = {
        'Pid': int(group['Pid'].iloc[0]),
        'id': int(group['id'].iloc[0]),
        'sub': process_dataframe(group[['startT', 'startXMin', 'startXMax', 'endT', 'endXMin', 'endXMax', 'LFunc', 'RFunc']]).to_dict('records')
    }
    holds_data.append(hold)

# 处理 slides 数据
slides_data = process_dataframe(slides_df[['startT', 'startX', 'Size', 'Pid']]).to_dict('records')

# 处理 flicks 数据
flicks_data = process_dataframe(flicks_df[['startT', 'startX', 'Size', 'Dir', 'Pid']]).to_dict('records')

# 处理 stars 数据
stars_data = []
for _, group in stars_df.groupby('id'):
    star = {
        'Pid': int(group['Pid'].iloc[0]),
        'id': int(group['id'].iloc[0]),
        'headT': round(float(group['headT'].iloc[0]), 3),
        'sub': process_dataframe(group[['startT', 'endT', 'startX', 'startY', 'endX', 'endY', 'Func']]).to_dict('records')
    }
    stars_data.append(star)

# 构建最终的 JSON 数据
result = {
    'planes': planes_data,
    'taps': taps_data,
    'holds': holds_data,
    'slides': slides_data,
    'flicks': flicks_data,
    'stars': stars_data
}

# 将结果保存为 JSON 文件
with open('Chart.json', 'w') as f:
    json.dump(result, f, indent=4)