# Albedo / diffuse (ETC1S, lossy, smallest)
toktx --bcmp --lower_left_maps_to_s0t0 output.ktx2 input.png

# Normal / metallic / detail (UASTC, high fidelity)
toktx --encode uastc --uastc_quality 2 --t2 --lower_left_maps_to_s0t0 output.ktx2 input.png

# Fix "ICC profile not found" errors
toktx --bcmp --assign_oetf srgb --lower_left_maps_to_s0t0 output.ktx2 input.png

# Linear data (normal maps, masks)
toktx --bcmp --assign_oetf linear --lower_left_maps_to_s0t0 output.ktx2 input.png
