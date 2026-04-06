import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../main.dart';
import '../../core/api_client.dart';
import '../../core/auth_provider.dart';

class ProductEditScreen extends StatefulWidget {
  final int? productId;

  const ProductEditScreen({super.key, this.productId});

  @override
  State<ProductEditScreen> createState() => _ProductEditScreenState();
}

class _ProductEditScreenState extends State<ProductEditScreen> {
  final _codeController = TextEditingController();
  final _nameController = TextEditingController();
  final _modelController = TextEditingController();
  final _priceController = TextEditingController();
  final _quantityController = TextEditingController();
  int? _supplierId;
  List<dynamic> _suppliers = [];
  bool _loading = true;
  String? _error;
  String? _imageUrlFromServer;
  String? _pickedImagePath;
  bool _uploadingImage = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  static int? _parseId(dynamic v) {
    if (v == null) return null;
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v.toString());
  }

  Future<void> _loadData() async {
    setState(() => _loading = true);
    try {
      _suppliers = await context.read<AuthProvider>().api.getSuppliers();
      if (_suppliers.isNotEmpty && _supplierId == null) _supplierId = _suppliers.first['id'] as int?;
      if (widget.productId != null) {
        final p = await context.read<AuthProvider>().api.getProduct(widget.productId!);
        _codeController.text = p['code']?.toString() ?? '';
        _nameController.text = p['name']?.toString() ?? '';
        _modelController.text = p['model']?.toString() ?? '';
        _supplierId = _parseId(p['supplierId']);
        _priceController.text = p['price']?.toString() ?? '';
        _quantityController.text = p['quantity']?.toString() ?? '0';
        _imageUrlFromServer = p['imageUrl']?.toString();
        if (_imageUrlFromServer != null && _imageUrlFromServer!.isEmpty) _imageUrlFromServer = null;
      }
    } catch (e) {
      setState(() => _error = apiErrorMessage(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickImage() async {
    final x = await ImagePicker().pickImage(source: ImageSource.gallery, maxWidth: 1920, imageQuality: 85);
    if (x == null || !mounted) return;
    setState(() {
      _pickedImagePath = x.path;
      _error = null;
    });
  }

  Future<void> _removePhoto() async {
    final api = context.read<AuthProvider>().api;
    setState(() => _error = null);
    if (_pickedImagePath != null) {
      setState(() => _pickedImagePath = null);
      return;
    }
    if (widget.productId == null || _imageUrlFromServer == null) return;
    setState(() => _uploadingImage = true);
    try {
      await api.deleteProductImage(widget.productId!);
      if (!mounted) return;
      setState(() {
        _imageUrlFromServer = null;
        _uploadingImage = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _error = apiErrorMessage(e);
        _uploadingImage = false;
      });
    }
  }

  String? _previewUrl(AuthProvider auth) {
    if (_pickedImagePath != null) return null;
    return auth.api.productImageAbsoluteUrl(_imageUrlFromServer);
  }

  Future<void> _save() async {
    if (_supplierId == null) {
      setState(() => _error = 'Выберите поставщика');
      return;
    }
    setState(() => _error = null);
    try {
      final auth = context.read<AuthProvider>();
      final api = auth.api;
      final dto = {
        'code': _codeController.text.trim(),
        'name': _nameController.text.trim(),
        'model': _modelController.text.trim().isEmpty ? null : _modelController.text.trim(),
        'supplierId': _supplierId,
        'price': double.tryParse(_priceController.text.replaceAll(',', '.')) ?? 0,
        'quantity': int.tryParse(_quantityController.text) ?? 0,
      };
      int? savedId = widget.productId;
      if (widget.productId != null) {
        await api.updateProduct(widget.productId!, dto);
      } else {
        final res = await api.createProduct(dto);
        savedId = _parseId(res['id'] ?? res['Id']);
      }
      if (_pickedImagePath != null && savedId != null) {
        setState(() => _uploadingImage = true);
        final updated = await api.uploadProductImage(savedId, _pickedImagePath!);
        if (mounted) {
          setState(() {
            _imageUrlFromServer = updated['imageUrl']?.toString();
            _pickedImagePath = null;
            _uploadingImage = false;
          });
        }
      }
      if (!mounted) return;
      Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = apiErrorMessage(e);
          _uploadingImage = false;
        });
      }
    }
  }

  @override
  void dispose() {
    _codeController.dispose();
    _nameController.dispose();
    _modelController.dispose();
    _priceController.dispose();
    _quantityController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final cs = Theme.of(context).colorScheme;
    if (_loading) {
      return buildAppScaffold(
        context,
        title: widget.productId == null ? 'Новый товар' : 'Редактирование',
        body: const Center(child: CircularProgressIndicator()),
      );
    }
    final absolutePreview = _previewUrl(auth);
    return buildAppScaffold(
      context,
      title: widget.productId == null ? 'Новый товар' : 'Редактирование товара',
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Фото для карточки', style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: AspectRatio(
                aspectRatio: 16 / 10,
                child: _pickedImagePath != null
                    ? Image.file(File(_pickedImagePath!), fit: BoxFit.cover)
                    : absolutePreview != null
                        ? Image.network(
                            absolutePreview,
                            fit: BoxFit.cover,
                            errorBuilder: (_, __, ___) => ColoredBox(
                              color: cs.surfaceContainerHighest,
                              child: Icon(Icons.broken_image_outlined, color: cs.onSurfaceVariant),
                            ),
                          )
                        : ColoredBox(
                            color: cs.surfaceContainerHighest,
                            child: Center(
                              child: Text('Нет фото', style: TextStyle(color: cs.onSurfaceVariant)),
                            ),
                          ),
              ),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: FilledButton.tonal(
                    onPressed: _uploadingImage ? null : _pickImage,
                    child: const Text('Выбрать из галереи'),
                  ),
                ),
                if (_pickedImagePath != null || _imageUrlFromServer != null) ...[
                  const SizedBox(width: 8),
                  IconButton(
                    onPressed: _uploadingImage ? null : _removePhoto,
                    icon: const Icon(Icons.delete_outline_rounded),
                    tooltip: 'Убрать фото',
                  ),
                ],
              ],
            ),
            if (_uploadingImage) const LinearProgressIndicator(),
            const SizedBox(height: 20),
            TextField(controller: _codeController, decoration: const InputDecoration(labelText: 'Код')),
            const SizedBox(height: 12),
            TextField(controller: _nameController, decoration: const InputDecoration(labelText: 'Название *')),
            TextField(controller: _modelController, decoration: const InputDecoration(labelText: 'Модель')),
            DropdownButtonFormField<int?>(
              value: _supplierId,
              decoration: const InputDecoration(labelText: 'Поставщик *'),
              items: [
                const DropdownMenuItem<int?>(value: null, child: Text('— Выберите поставщика')),
                ..._suppliers.map<DropdownMenuItem<int?>>((s) => DropdownMenuItem(value: s['id'] as int, child: Text(s['name']?.toString() ?? ''))),
              ],
              onChanged: (v) => setState(() => _supplierId = v),
            ),
            TextField(controller: _priceController, decoration: const InputDecoration(labelText: 'Цена'), keyboardType: TextInputType.number),
            TextField(controller: _quantityController, decoration: const InputDecoration(labelText: 'Количество'), keyboardType: TextInputType.number),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(_error!, style: TextStyle(color: cs.error)),
              ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: _uploadingImage ? null : _save,
              child: const Text('Сохранить'),
            ),
          ],
        ),
      ),
    );
  }
}
