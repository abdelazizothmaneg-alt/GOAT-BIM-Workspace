import math
import os

def generate_door_files(output_dir, X=1000.0):
    os.makedirs(output_dir, exist_ok=True)
    
    # Proportions according to the image:
    # Width = X
    # Door Leaf Height = 2 * X
    # Total Height = 2.5 * X
    # Top Transom Height = 0.5 * X
    W = float(X)
    H_leaf = 2.0 * X
    H_total = 2.5 * X
    H_transom = 0.5 * X
    
    # Depth / Thickness:
    T_leaf = 50.0       # 50mm leaf thickness
    T_frame = 100.0     # 100mm wall jamb depth
    W_frame = 50.0      # 50mm frame width
    
    # Left cutout for glazed decorative panel:
    # Approximately 1/4 to 1/3 of door width, offset from left
    cutout_x0 = 0.12 * W
    cutout_x1 = 0.38 * W
    cutout_z0 = 0.10 * H_leaf
    cutout_z1 = 0.90 * H_leaf
    
    # Mesh data structures
    # We will accumulate triangles / quads for OBJ and DXF
    # items: list of dict(layer=str, faces=[list of (x, y, z)])
    layers_data = {}
    
    def add_quad(layer, p1, p2, p3, p4):
        if layer not in layers_data:
            layers_data[layer] = []
        layers_data[layer].append([p1, p2, p3, p4])
        
    def add_box(layer, x0, x1, y0, y1, z0, z1):
        # 6 faces of box
        # Bottom (z0)
        add_quad(layer, (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0))
        # Top (z1)
        add_quad(layer, (x0, y0, z1), (x0, y1, z1), (x1, y1, z1), (x1, y0, z1))
        # Front (y0)
        add_quad(layer, (x0, y0, z0), (x0, y0, z1), (x1, y0, z1), (x1, y0, z0))
        # Back (y1)
        add_quad(layer, (x1, y1, z0), (x1, y1, z1), (x0, y1, z1), (x0, y1, z0))
        # Left (x0)
        add_quad(layer, (x0, y1, z0), (x0, y1, z1), (x0, y0, z1), (x0, y0, z0))
        # Right (x1)
        add_quad(layer, (x1, y0, z0), (x1, y0, z1), (x1, y1, z1), (x1, y1, z0))

    # 1. Door Leaf (Lower Part: Z from 0 to H_leaf)
    # The leaf has a cutout from (cutout_x0, cutout_z0) to (cutout_x1, cutout_z1)
    # Left vertical column of leaf
    add_box("DOOR_LEAF", 0.0, cutout_x0, 0.0, T_leaf, 0.0, H_leaf)
    # Right panel of leaf
    add_box("DOOR_LEAF", cutout_x1, W, 0.0, T_leaf, 0.0, H_leaf)
    # Bottom strip under cutout
    add_box("DOOR_LEAF", cutout_x0, cutout_x1, 0.0, T_leaf, 0.0, cutout_z0)
    # Top strip above cutout
    add_box("DOOR_LEAF", cutout_x0, cutout_x1, 0.0, T_leaf, cutout_z1, H_leaf)
    
    # Recessed inner border on the door leaf (decorative groove)
    groove_offset = 25.0
    groove_w = 12.0
    groove_depth = 4.0
    # Front groove strips
    add_box("DOOR_TRIM", groove_offset, W - groove_offset, -groove_depth, 0.0, groove_offset, groove_offset + groove_w)
    add_box("DOOR_TRIM", groove_offset, W - groove_offset, -groove_depth, 0.0, H_leaf - groove_offset - groove_w, H_leaf - groove_offset)
    add_box("DOOR_TRIM", groove_offset, groove_offset + groove_w, -groove_depth, 0.0, groove_offset, H_leaf - groove_offset)
    add_box("DOOR_TRIM", W - groove_offset - groove_w, W - groove_offset, -groove_depth, 0.0, groove_offset, H_leaf - groove_offset)

    # 2. Top Transom Panel (Upper Part: Z from H_leaf to H_total)
    add_box("DOOR_TRANSOM", 0.0, W, 0.0, T_leaf, H_leaf, H_total)
    
    # 3. Outer Frame / Jamb (surrounds opening on left, right, top)
    y_f0 = -(T_frame - T_leaf) / 2.0
    y_f1 = y_f0 + T_frame
    # Left jamb
    add_box("DOOR_FRAME", -W_frame, 0.0, y_f0, y_f1, 0.0, H_total + W_frame)
    # Right jamb
    add_box("DOOR_FRAME", W, W + W_frame, y_f0, y_f1, 0.0, H_total + W_frame)
    # Top header
    add_box("DOOR_FRAME", -W_frame, W + W_frame, y_f0, y_f1, H_total, H_total + W_frame)

    # 4. Glass Panel inside cutout
    t_glass = 8.0
    y_g0 = (T_leaf - t_glass) / 2.0
    y_g1 = y_g0 + t_glass
    add_box("DOOR_GLASS", cutout_x0, cutout_x1, y_g0, y_g1, cutout_z0, cutout_z1)
    
    # Inner frame / glazing bead around cutout
    bead_w = 15.0
    bead_thick = 10.0
    add_box("DOOR_TRIM", cutout_x0 - bead_w, cutout_x0, y_g0 - bead_thick, y_g1 + bead_thick, cutout_z0 - bead_w, cutout_z1 + bead_w)
    add_box("DOOR_TRIM", cutout_x1, cutout_x1 + bead_w, y_g0 - bead_thick, y_g1 + bead_thick, cutout_z0 - bead_w, cutout_z1 + bead_w)
    add_box("DOOR_TRIM", cutout_x0, cutout_x1, y_g0 - bead_thick, y_g1 + bead_thick, cutout_z0 - bead_w, cutout_z0)
    add_box("DOOR_TRIM", cutout_x0, cutout_x1, y_g0 - bead_thick, y_g1 + bead_thick, cutout_z1, cutout_z1 + bead_w)

    # 5. 6 Concentric Diamond Motifs stacked vertically
    num_diamonds = 6
    cutout_h = cutout_z1 - cutout_z0
    slot_h = cutout_h / num_diamonds
    cx = (cutout_x0 + cutout_x1) / 2.0
    slot_w = cutout_x1 - cutout_x0
    
    y_d0 = y_g0 - 6.0
    y_d1 = y_g1 + 6.0
    
    def add_diamond_ring(center_x, center_z, rx_out, rz_out, rx_in, rz_in, yd0, yd1):
        # 4 segments forming diamond ring
        # Points: Top, Right, Bottom, Left
        # Outer:
        pt_top_o = (center_x, center_z + rz_out)
        pt_rt_o  = (center_x + rx_out, center_z)
        pt_bt_o  = (center_x, center_z - rz_out)
        pt_lt_o  = (center_x - rx_out, center_z)
        # Inner:
        pt_top_i = (center_x, center_z + rz_in)
        pt_rt_i  = (center_x + rx_in, center_z)
        pt_bt_i  = (center_x, center_z - rz_in)
        pt_lt_i  = (center_x - rx_in, center_z)
        
        segments = [
            (pt_top_o, pt_rt_o, pt_rt_i, pt_top_i),
            (pt_rt_o, pt_bt_o, pt_bt_i, pt_rt_i),
            (pt_bt_o, pt_lt_o, pt_lt_i, pt_bt_i),
            (pt_lt_o, pt_top_o, pt_top_i, pt_lt_i)
        ]
        
        for p1_2d, p2_2d, p3_2d, p4_2d in segments:
            # Front face
            add_quad("DOOR_DIAMONDS", (p1_2d[0], yd0, p1_2d[1]), (p2_2d[0], yd0, p2_2d[1]), (p3_2d[0], yd0, p3_2d[1]), (p4_2d[0], yd0, p4_2d[1]))
            # Back face
            add_quad("DOOR_DIAMONDS", (p4_2d[0], yd1, p4_2d[1]), (p3_2d[0], yd1, p3_2d[1]), (p2_2d[0], yd1, p2_2d[1]), (p1_2d[0], yd1, p1_2d[1]))
            # Outer edge
            add_quad("DOOR_DIAMONDS", (p1_2d[0], yd0, p1_2d[1]), (p1_2d[0], yd1, p1_2d[1]), (p2_2d[0], yd1, p2_2d[1]), (p2_2d[0], yd0, p2_2d[1]))
            # Inner edge
            add_quad("DOOR_DIAMONDS", (p3_2d[0], yd0, p3_2d[1]), (p3_2d[0], yd1, p3_2d[1]), (p4_2d[0], yd1, p4_2d[1]), (p4_2d[0], yd0, p4_2d[1]))

    for i in range(num_diamonds):
        cz = cutout_z0 + (i + 0.5) * slot_h
        rx_max = (slot_w * 0.44)
        rz_max = (slot_h * 0.44)
        
        # Outer diamond
        add_diamond_ring(cx, cz, rx_max, rz_max, rx_max - 10.0, rz_max - 10.0, y_d0, y_d1)
        # Middle diamond
        add_diamond_ring(cx, cz, rx_max * 0.68, rz_max * 0.68, rx_max * 0.68 - 8.0, rz_max * 0.68 - 8.0, y_d0, y_d1)
        # Center solid diamond
        # Small inner diamond
        rx_in = rx_max * 0.35
        rz_in = rz_max * 0.35
        add_diamond_ring(cx, cz, rx_in, rz_in, rx_in - 6.0, rz_in - 6.0, y_d0, y_d1)

    # -------------------------------------------------------------
    # Write DXF File (Universal 3D DXF with 3DFACE entities & layers)
    # -------------------------------------------------------------
    dxf_path = os.path.join(output_dir, f"Parametric_Door_X_{int(X)}mm.dxf")
    
    # Layer colors for DXF:
    layer_colors = {
        "DOOR_LEAF": 30,       # Brown/Wood
        "DOOR_TRANSOM": 34,    # Darker Wood/Matching
        "DOOR_FRAME": 8,       # Dark Gray Frame
        "DOOR_TRIM": 40,       # Trim color
        "DOOR_GLASS": 150,     # Cyan/Glass
        "DOOR_DIAMONDS": 7     # White/Silver Muntin
    }
    
    with open(dxf_path, "w", encoding="ascii") as f:
        # DXF Header
        f.write("0\nSECTION\n2\nHEADER\n")
        f.write("9\n$INSUNITS\n70\n4\n") # 4 = Millimeters
        f.write("0\nENDSEC\n")
        
        # Tables section: Layers
        f.write("0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nLAYER\n")
        for lyr, col in layer_colors.items():
            f.write(f"0\nLAYER\n2\n{lyr}\n70\n0\n62\n{col}\n6\nCONTINUOUS\n")
        f.write("0\nENDTAB\n0\nENDSEC\n")
        
        # Entities section
        f.write("0\nSECTION\n2\nENTITIES\n")
        for lyr, quads in layers_data.items():
            for q in quads:
                f.write(f"0\n3DFACE\n8\n{lyr}\n")
                f.write(f"10\n{q[0][0]:.3f}\n20\n{q[0][1]:.3f}\n30\n{q[0][2]:.3f}\n")
                f.write(f"11\n{q[1][0]:.3f}\n21\n{q[1][1]:.3f}\n31\n{q[1][2]:.3f}\n")
                f.write(f"12\n{q[2][0]:.3f}\n22\n{q[2][1]:.3f}\n32\n{q[2][2]:.3f}\n")
                f.write(f"13\n{q[3][0]:.3f}\n23\n{q[3][1]:.3f}\n33\n{q[3][2]:.3f}\n")
        f.write("0\nENDSEC\n0\nEOF\n")

    # -------------------------------------------------------------
    # Write OBJ & MTL Files (Wavefront OBJ for direct Revit / 3D import)
    # -------------------------------------------------------------
    obj_path = os.path.join(output_dir, f"Parametric_Door_X_{int(X)}mm.obj")
    mtl_path = os.path.join(output_dir, f"Parametric_Door_X_{int(X)}mm.mtl")
    
    with open(mtl_path, "w", encoding="utf-8") as f:
        f.write("""newmtl DOOR_LEAF
Kd 0.65 0.50 0.38
Ka 0.20 0.15 0.10
d 1.0

newmtl DOOR_TRANSOM
Kd 0.60 0.46 0.35
Ka 0.20 0.15 0.10
d 1.0

newmtl DOOR_FRAME
Kd 0.30 0.30 0.30
Ka 0.10 0.10 0.10
d 1.0

newmtl DOOR_TRIM
Kd 0.50 0.38 0.28
Ka 0.15 0.10 0.08
d 1.0

newmtl DOOR_GLASS
Kd 0.70 0.85 0.90
Ka 0.20 0.30 0.35
d 0.35

newmtl DOOR_DIAMONDS
Kd 0.85 0.85 0.85
Ka 0.40 0.40 0.40
d 1.0
""")

    with open(obj_path, "w", encoding="utf-8") as f:
        f.write(f"# Parametric Door Generated Model (X={int(X)}mm)\n")
        f.write(f"mtllib {os.path.basename(mtl_path)}\n")
        
        v_offset = 1
        for lyr, quads in layers_data.items():
            f.write(f"g {lyr}\nusemtl {lyr}\n")
            for q in quads:
                for pt in q:
                    # In OBJ standard: Y is UP, Z is DEPTH, X is WIDTH
                    # Or keep X=X, Y=Depth, Z=Elevation
                    # For Revit: Revit OBJ importer uses Z as elevation!
                    f.write(f"v {pt[0]:.3f} {pt[1]:.3f} {pt[2]:.3f}\n")
                f.write(f"f {v_offset} {v_offset+1} {v_offset+2} {v_offset+3}\n")
                v_offset += 4

    print(f"Generated DXF: {dxf_path}")
    print(f"Generated OBJ: {obj_path}")
    return dxf_path, obj_path

if __name__ == "__main__":
    out_dir = "C:/Users/faqih/.gemini/antigravity/scratch/Revit_Door_Family"
    for x_val in [900.0, 1000.0, 1100.0, 1200.0]:
        generate_door_files(out_dir, X=x_val)
