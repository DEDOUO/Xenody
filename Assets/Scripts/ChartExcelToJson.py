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

# 处理 planes 数据
planes_data = []
for _, row in planes_df.groupby('id'):
    plane = {
        'id': int(row['id'].iloc[0]),
       'sub': row[['startT','startY', 'endT', 'endY', 'Func']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')
    }
    planes_data.append(plane)


# 处理 taps 数据
taps_data = taps_df[['startT','startX', 'Size', 'Pid']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')


# 处理 holds 数据
holds_data = []
for _, row in holds_df.groupby('id'):
    hold = {
        'Pid': int(row['Pid'].iloc[0]),
        'id': int(row['id'].iloc[0]),
       'sub': row[['startT','startXMin','startXMax', 'endT', 'endXMin', 'endXMax', 'LFunc', 'RFunc']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')
    }
    holds_data.append(hold)


# 处理 slides 数据
slides_data = slides_df[['startT','startX', 'Size', 'Pid']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')


# 处理 flicks 数据
flicks_data = flicks_df[['startT','startX', 'Size', 'Dir', 'Pid']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')


# 处理 stars 数据
stars_data = []
for _, row in stars_df.groupby('id'):
    star = {
        'Pid': int(row['Pid'].iloc[0]),
        'id': int(row['id'].iloc[0]),
        'headT': float(row['headT'].iloc[0]),
       'sub': row[['startT', 'endT','startX','startY', 'endX', 'endY', 'Func']].applymap(lambda x: int(x) if isinstance(x, np.int64) else x).to_dict('records')
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